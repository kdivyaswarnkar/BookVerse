namespace BookStore.Core.Entities;

public class Book
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? Isbn { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsDeleted { get; set; }

    // audit columns
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public Category Category { get; set; } = null!;
}