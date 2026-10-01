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

public class AuthService(AppDbContext db, IOptions<AppOptions> options) : IAuthService
{
    public async Task RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new AppException("Email already registered", 409);

        // random 64-character token for the verification link
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),   // never store the real password
            VerificationToken = token
        };
        db.Users.Add(user);

        var link = $"{options.Value.FrontendUrl}/verify-email?token={token}";
        db.EmailQueue.Add(new EmailQueueItem
        {
            ToEmail = email,
            Subject = "Verify your BookStore email",
            Body = $"Hi {user.Name},\n\nPlease verify your email by opening this link:\n{link}\n\n" +
                   "If you did not create this account, you can ignore this email."
        });

        try
        {
            // user and email are saved together in one transaction
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // two people registered the same email at the same moment
            throw new AppException("Email already registered", 409);
        }
    }
}