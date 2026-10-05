namespace BookStore.Core.Entities;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Book Book { get; set; } = null!;
}
