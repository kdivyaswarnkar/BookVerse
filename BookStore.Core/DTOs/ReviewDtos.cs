using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

public class SaveReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public string Reviewer { get; set; } = "";
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool VerifiedPurchase { get; set; }   // reviewer has a paid order with this book
    public DateTime CreatedAt { get; set; }
}

public class BookReviewsDto
{
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public PagedResult<ReviewDto> Reviews { get; set; } = new();
}

public class WishlistItemDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime AddedAt { get; set; }
}
