using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

public class AddressRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = "";

    [Required]
    [RegularExpression(@"^\+?[0-9 \-]{7,15}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";

    [Required, StringLength(200)]
    public string Line1 { get; set; } = "";

    [Required, StringLength(100)]
    public string City { get; set; } = "";

    [Required, StringLength(100)]
    public string State { get; set; } = "";

    [Required]
    [RegularExpression(@"^[A-Za-z0-9 \-]{3,10}$", ErrorMessage = "Enter a valid postal code.")]
    public string PostalCode { get; set; } = "";
}

public class AddressDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Line1 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
}