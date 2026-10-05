namespace BookStore.Core.Entities;

// Orders are CREATED by the stored procedure sales.sp_PlaceOrder (one safe transaction).
// EF Core is used to READ them and to change their status.
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AddressId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? CouponId { get; set; }           // the coupon used (null = none)
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }          // the user (customer or admin) who last changed the order

    public Address Address { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }

    // the price at the moment of purchase: later price changes never touch old orders
    public decimal UnitPrice { get; set; }

    public Book Book { get; set; } = null!;
}
