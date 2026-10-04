namespace BookStore.Core.Entities;

// One payment record per order (the table has a unique OrderId).
// A failed payment is retried by reusing the same row with a new provider payment id.
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Provider { get; set; } = "";
    public string? ProviderPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Created";   // Created, Paid, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}