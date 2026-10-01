using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task VerifyEmailAsync(string token);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllAsync(int userId);
    // BE-06 adds: forgot password, reset password, resend verification email
}