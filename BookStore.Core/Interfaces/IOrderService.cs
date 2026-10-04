using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(int userId, int addressId);
    Task<List<OrderSummaryDto>> ListAsync(int userId);
    Task<OrderDetailDto?> GetAsync(int userId, int orderId);
}