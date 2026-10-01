using System.Security.Cryptography;
using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookStore.Data.Services;

public class AuthService(
    AppDbContext db,
    ITokenService tokens,
    IOptions<AppOptions> appOptions,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    // Used to keep login timing the same when the email does not exist
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("not-a-real-password");

    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    // ---------- Register ----------
    public async Task RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new AppException("Email already registered", 409);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            VerificationToken = NewToken()
        };
        db.Users.Add(user);
        QueueVerificationEmail(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new AppException("Email already registered", 409);
        }
    }

    // ---------- Verify email ----------
    public async Task VerifyEmailAsync(string token)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token && !u.IsDeleted);

        if (user is null)
            throw new AppException("This verification link is invalid or has already been used.", 400);

        user.EmailVerified = true;
        user.VerificationToken = null;   // a link works only once
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ---------- Resend verification email ----------
    public async Task ResendVerificationAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized && !u.IsDeleted);

        // no such account, or already verified: do nothing, and the caller gets the same answer
        if (user is null || user.EmailVerified) return;

        user.VerificationToken = NewToken();   // the older link stops working
        user.UpdatedAt = DateTime.UtcNow;
        QueueVerificationEmail(user);
        await db.SaveChangesAsync();
    }

    // ---------- Login ----------
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        // always run one password check, so the response time does not reveal if the email exists
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);

        if (user is null || !passwordOk)
            throw new AppException("Invalid email or password", 401);

        if (!user.EmailVerified)
            throw new AppException("Please verify your email before logging in.", 403);

        return await IssueTokensAsync(user);
    }

    // ---------- Refresh ----------
    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var hash = tokens.HashToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hash);

        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow || stored.User.IsDeleted)
            throw new AppException("Session expired. Please log in again.", 401);

        if (stored.RevokedAt is not null)
        {
            // an already-used token came back: it may have been stolen, so end every session of this user
            await RevokeAllAsync(stored.UserId);
            throw new AppException("Session expired. Please log in again.", 401);
        }

        stored.RevokedAt = DateTime.UtcNow;            // each refresh token works only once
        return await IssueTokensAsync(stored.User);    // saves the revoke and the new token together
    }

    // ---------- Logout ----------
    public async Task LogoutAsync(string refreshToken)
    {
        var hash = tokens.HashToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == hash && t.RevokedAt == null);

        if (stored is null) return;   // already logged out: nothing to do

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public Task LogoutAllAsync(int userId) => RevokeAllAsync(userId);

    // ---------- Forgot password ----------
    public async Task ForgotPasswordAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized && !u.IsDeleted);

        // unknown email: do nothing, and the caller gets the same answer
        if (user is null) return;

        var rawToken = NewToken();
        user.ResetToken = tokens.HashToken(rawToken);   // only the hash is stored
        user.ResetTokenExpiry = DateTime.UtcNow.Add(ResetTokenLifetime);
        user.UpdatedAt = DateTime.UtcNow;

        var link = $"{appOptions.Value.FrontendUrl}/reset-password?token={rawToken}";
        db.EmailQueue.Add(new EmailQueueItem
        {
            ToEmail = user.Email,
            Subject = "Reset your BookStore password",
            Body = $"Hi {user.Name},\n\nWe received a request to reset your password. " +
                   $"Open this link within {ResetTokenLifetime.TotalMinutes:0} minutes:\n{link}\n\n" +
                   "If you did not ask for this, ignore this email. Your password will not change."
        });

        await db.SaveChangesAsync();
    }

    // ---------- Reset password ----------
    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var hash = tokens.HashToken(request.Token);
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.ResetToken == hash && u.ResetTokenExpiry > DateTime.UtcNow && !u.IsDeleted);

        if (user is null)
            throw new AppException("This reset link is invalid or has expired.", 400);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetToken = null;          // a link works only once
        user.ResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await RevokeAllAsync(user.Id);   // log the user out on every device
    }

    // ---------- helpers ----------
    private static string NewToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private void QueueVerificationEmail(User user)
    {
        var link = $"{appOptions.Value.FrontendUrl}/verify-email?token={user.VerificationToken}";
        db.EmailQueue.Add(new EmailQueueItem
        {
            ToEmail = user.Email,
            Subject = "Verify your BookStore email",
            Body = $"Hi {user.Name},\n\nPlease verify your email by opening this link:\n{link}\n\n" +
                   "If you did not create this account, you can ignore this email."
        });
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user);
        var refreshToken = tokens.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays)
        });
        await db.SaveChangesAsync();

        return new AuthResponse(
            accessToken, expiresAt, refreshToken,
            new UserInfo(user.Id, user.Name, user.Email, user.Role));
    }

    private async Task RevokeAllAsync(int userId)
    {
        var now = (DateTime?)DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }
}