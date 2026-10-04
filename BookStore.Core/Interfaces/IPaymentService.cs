using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(int userId, int orderId);
    Task<PaymentStatusDto?> GetStatusAsync(int userId, int orderId);

    // Returns false when the signature is invalid (the controller answers 400)
    Task<bool> HandleWebhookAsync(string body, string? signature);
}