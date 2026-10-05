using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class OrderAdminService(AppDbContext db, IOrderService orders) : IOrderAdminService
{
    public async Task<PagedResult<AdminOrderListItemDto>> ListAsync(string? status, int page, int pageSize)
    {
        var query =
            from o in db.Orders.AsNoTracking()
            join u in db.Users on o.UserId equals u.Id
            select new { Order = o, User = u };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Order.Status == status);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Order.CreatedAt).ThenByDescending(x => x.Order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminOrderListItemDto
            {
                Id = x.Order.Id,
                Customer = x.User.Name,
                Email = x.User.Email,
                TotalAmount = x.Order.TotalAmount,
                Status = x.Order.Status,
                CreatedAt = x.Order.CreatedAt,
                ItemCount = x.Order.Items.Sum(i => i.Quantity)
            })
            .ToListAsync();

        return new PagedResult<AdminOrderListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<AdminOrderDetailDto?> GetAsync(int orderId)
    {
        var order = await (
            from o in db.Orders.AsNoTracking()
            join u in db.Users on o.UserId equals u.Id
            where o.Id == orderId
            select new AdminOrderDetailDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                Customer = u.Name,
                Email = u.Email,
                ShipTo = new AddressDto
                {
                    Id = o.Address.Id,
                    FullName = o.Address.FullName,
                    Phone = o.Address.Phone,
                    Line1 = o.Address.Line1,
                    City = o.Address.City,
                    State = o.Address.State,
                    PostalCode = o.Address.PostalCode
                },
                Items = o.Items.Select(i => new OrderItemDto
                {
                    BookId = i.BookId,
                    Title = i.Book.Title,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.UnitPrice * i.Quantity
                }).ToList()
            }).FirstOrDefaultAsync();

        if (order is null) return null;

        order.PaymentStatus = await db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .Select(p => p.Status)
            .FirstOrDefaultAsync() ?? "NotStarted";

        return order;
    }

    public async Task UpdateStatusAsync(int adminId, int orderId, string newStatus)
    {
        // cancelling also returns the stock, so it has its own careful method
        if (newStatus == OrderStatuses.Cancelled)
        {
            await orders.CancelAsAdminAsync(adminId, orderId);
            return;
        }

        var current = await db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => o.Status)
            .FirstOrDefaultAsync();

        if (current is null)
            throw new AppException("Order not found.", 404);

        if (!OrderStatuses.CanMove(current, newStatus))
            throw new AppException($"An order that is {current} cannot become {newStatus}.", 400);

        var now = (DateTime?)DateTime.UtcNow;
        var by = (int?)adminId;

        // COMPARE-AND-SET: change it only if the status is still what we just read
        var rows = await db.Orders
            .Where(o => o.Id == orderId && o.Status == current)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.UpdatedAt, now)
                .SetProperty(o => o.UpdatedBy, by));

        if (rows == 0)
            throw new AppException("The order was changed by someone else. Reload it and try again.", 409);
    }
}