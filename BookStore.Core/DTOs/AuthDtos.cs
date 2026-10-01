using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

// One place for the rules, so register and reset-password always agree
public static class ValidationPatterns
{
    public const string Email = @"^[A-Za-z0-9._%+-]+@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$";
    public const string EmailMessage = "Enter a valid email address, for example name@example.com";

    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$";
    public const string PasswordMessage =
        "Password must be 8-100 characters with an uppercase letter, a lowercase letter and a number.";
}

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, StringLength(200)]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    public string Email { get; set; } = "";

    [Required]
    [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordMessage)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required, StringLength(200)]
    public string Email { get; set; } = "";

    [Required, StringLength(100)]
    public string Password { get; set; } = "";
}

// Used by forgot-password and resend-verification
public class EmailRequest
{
    [Required, StringLength(200)]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    [Required, StringLength(200)]
    public string Token { get; set; } = "";

    [Required]
    [RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordMessage)]
    public string NewPassword { get; set; } = "";
}

public class VerifyEmailRequest
{
    [Required, StringLength(200)]
    public string Token { get; set; } = "";
}

// Used by both /refresh and /logout
public class RefreshTokenRequest
{
    [Required, StringLength(500)]
    public string RefreshToken { get; set; } = "";
}

public record UserInfo(int Id, string Name, string Email, string Role);

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    UserInfo User);

public record MessageResponse(string Message);