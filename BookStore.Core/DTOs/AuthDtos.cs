using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, StringLength(200)]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$",
     ErrorMessage = "Enter a valid email address, for example name@example.com")]
    public string Email { get; set; } = "";

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8-100 characters with an uppercase letter, a lowercase letter and a number.")]
    public string Password { get; set; } = "";
}

public record MessageResponse(string Message);