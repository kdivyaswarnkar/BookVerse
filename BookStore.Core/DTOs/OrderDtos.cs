using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

public class PlaceOrderRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a delivery address.")]
    public int AddressId { get; set; }
}

public record PlaceOrderResponse(int OrderId, decimal TotalAmount, string Status);

public class OrderSummaryDto
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class OrderItemDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public AddressDto ShipTo { get; set; } = new();
    public List<OrderItemDto> Items { get; set; } = new();
}