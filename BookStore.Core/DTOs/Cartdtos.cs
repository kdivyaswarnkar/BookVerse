using System.ComponentModel.DataAnnotations;
using BookStore.Core.Constants;

namespace BookStore.Core.DTOs;

public class AddToCartRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a book.")]
    public int BookId { get; set; }
    [Range(1, CartRules.MaxQuantityPerItem, ErrorMessage = "Quantity must be between 1 and 10.")]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, CartRules.MaxQuantityPerItem, ErrorMessage = "Quantity must be between 1 and 10. To remove the book, delete the item.")]
    public int Quantity { get; set; }
}


public class CartItemDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }        // current price of one copy
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }    // Price x Quantity
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }     // false when stock has dropped below the quantity
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();

    // calculated from the items, so they can never disagree with them
    public int ItemCount => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public bool HasUnavailableItems => Items.Any(i => !i.IsAvailable);
}