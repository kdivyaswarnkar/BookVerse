using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(int userId, int addressId, string? couponCode = null);
    Task<List<OrderSummaryDto>> ListAsync(int userId);
    Task<OrderDetailDto?> GetAsync(int userId, int orderId);

    // A customer cancels their own order (only while it is still Pending)
    Task CancelAsync(int userId, int orderId);

    // An admin cancels any order (also only while Pending)
    Task CancelAsAdminAsync(int adminId, int orderId);
}
