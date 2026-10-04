using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

// The only thing the rest of the app knows about a payment provider.
// Today: FakePaymentGateway. Later: a Razorpay or Stripe class with the same interface.
public interface IPaymentGateway
{
    string Name { get; }

    // Starts a payment with the provider and returns the provider's payment id
    Task<string> CreatePaymentAsync(decimal amount, int orderId);

    // Checks the signature and reads the message. Returns null when the signature is wrong.
    PaymentWebhookEvent? ReadWebhook(string body, string? signature);
}