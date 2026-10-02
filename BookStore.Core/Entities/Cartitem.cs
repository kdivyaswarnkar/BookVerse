namespace BookStore.Core.Entities;

// One line in a user's cart: this user wants this many copies of this book.
// The price is NOT stored here. The cart always shows the book's current price,
// and the price is frozen only when an order is placed (BE-09).
public class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Book Book { get; set; } = null!;
}