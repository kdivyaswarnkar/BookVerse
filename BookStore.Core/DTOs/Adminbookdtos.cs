using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

// Used for both "create book" and "update book"
public class BookUpsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    public int CategoryId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(150)]
    public string Author { get; set; } = "";

    [StringLength(20)]
    public string? Isbn { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100000.")]
    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int Stock { get; set; }

}