using System.ComponentModel.DataAnnotations;
using BookStore.Core.Constants;

namespace BookStore.Core.DTOs;

public class AdminOrderListItemDto
{
    public int Id { get; set; }
    public string Customer { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

// Everything the customer sees, plus who ordered and the payment state
public class AdminOrderDetailDto : OrderDetailDto
{
    public string Customer { get; set; } = "";
    public string Email { get; set; } = "";
    public string PaymentStatus { get; set; } = "";   // NotStarted, Created, Paid, Failed
}

public class UpdateOrderStatusRequest
{
    // Pending and Paid are not allowed here: Paid is set only by the payment webhook
    [Required]
    [AllowedValues(OrderStatuses.Shipped, OrderStatuses.Delivered, OrderStatuses.Cancelled,
        ErrorMessage = "Status must be Shipped, Delivered or Cancelled.")]
    public string Status { get; set; } = "";
}