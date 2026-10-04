using System.ComponentModel.DataAnnotations;

namespace BookStore.Core.DTOs;

public class CreatePaymentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose an order.")]
    public int OrderId { get; set; }
}

// The amount is NOT part of the request: it always comes from the order in the database.
public record CreatePaymentResponse(int OrderId, string Provider, string ProviderPaymentId, decimal Amount);

public class PaymentStatusDto
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = "";
    public string PaymentStatus { get; set; } = "";   // NotStarted, Created, Paid, Failed
}

// What the payment provider sends to our webhook
public record PaymentWebhookEvent(string ProviderPaymentId, string Status);   // Status: succeeded or failed

// Development only: lets you act as the payment provider
public class SimulatePaymentRequest
{
    [Required, StringLength(100)]
    public string ProviderPaymentId { get; set; } = "";

    public bool Success { get; set; } = true;

    // send a wrong signature, to see the webhook reject it
    public bool BadSignature { get; set; }
}