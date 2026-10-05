using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IOrderAdminService
{
    Task<PagedResult<AdminOrderListItemDto>> ListAsync(string? status, int page, int pageSize);
    Task<AdminOrderDetailDto?> GetAsync(int orderId);
    Task UpdateStatusAsync(int adminId, int orderId, string newStatus);
}