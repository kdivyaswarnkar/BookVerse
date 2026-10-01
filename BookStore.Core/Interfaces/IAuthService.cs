using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task VerifyEmailAsync(string token);
    Task ResendVerificationAsync(string email);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllAsync(int userId);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}