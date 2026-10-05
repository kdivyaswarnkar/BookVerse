using Microsoft.AspNetCore.RateLimiting;
using BookStore.Core.Constants;
using System.Security.Claims;
using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth) : ControllerBase
{
    // POST api/auth/register
    [EnableRateLimiting(RateLimitPolicies.Email)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        await auth.RegisterAsync(request);
        return StatusCode(201, new MessageResponse(
            "Registration successful. Please check your email to verify your account."));
    }

    // POST api/auth/verify-email   (the React page calls this with the token from the email link)
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        await auth.VerifyEmailAsync(request.Token);
        return Ok(new MessageResponse("Email verified. You can now log in."));
    }

    // POST api/auth/resend-verification
    [EnableRateLimiting(RateLimitPolicies.Email)]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(EmailRequest request)
    {
        await auth.ResendVerificationAsync(request.Email);
        // same answer whether or not the account exists
        return Ok(new MessageResponse(
            "If this account exists and is not verified yet, a new verification link has been sent."));
    }

    // POST api/auth/login
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request) =>
        Ok(await auth.LoginAsync(request));

    // POST api/auth/refresh
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request) =>
        Ok(await auth.RefreshAsync(request.RefreshToken));

    // POST api/auth/logout   (works even when the access token has already expired)
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        await auth.LogoutAsync(request.RefreshToken);
        return Ok(new MessageResponse("Logged out."));
    }

    // POST api/auth/logout-all   (ends the sessions on every device)
    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        await auth.LogoutAllAsync(CurrentUserId);
        return Ok(new MessageResponse("Logged out from all devices."));
    }

    // POST api/auth/forgot-password
    [EnableRateLimiting(RateLimitPolicies.Email)]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(EmailRequest request)
    {
        await auth.ForgotPasswordAsync(request.Email);
        // same answer whether or not the account exists
        return Ok(new MessageResponse("If this email is registered, a password reset link has been sent."));
    }

    // POST api/auth/reset-password
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        await auth.ResetPasswordAsync(request);
        return Ok(new MessageResponse("Password changed. Please log in with your new password."));
    }

    // GET api/auth/me   (needs a valid access token)
    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserInfo> Me() =>
        Ok(new UserInfo(
            CurrentUserId,
            User.FindFirstValue(ClaimTypes.Name)!,
            User.FindFirstValue(ClaimTypes.Email)!,
            User.FindFirstValue(ClaimTypes.Role)!));

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
