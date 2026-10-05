using BookStore.Core.Entities;
using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class OrderService(AppDbContext db) : IOrderService
{
    // PLACING an order is done by the stored procedure, in ONE database transaction:
    // check address + cart, lock the books, check stock, create order and items,
    // reduce stock, empty the cart. If anything fails, nothing is saved.
    public async Task<PlaceOrderResponse> PlaceOrderAsync(int userId, int addressId, string? couponCode = null)
    {
        var rows = await db.Database
            .SqlQuery<PlaceOrderRow>($"EXEC sales.sp_PlaceOrder {userId}, {addressId}, {couponCode}")   // values are sent as parameters
            .ToListAsync();

        var row = rows.Single();
        return new PlaceOrderResponse(row.OrderId, row.TotalAmount, OrderStatuses.Pending);
    }

    // READING orders is normal EF Core. Every query is filtered by the user id from the token.
    public Task<List<OrderSummaryDto>> ListAsync(int userId) =>
        db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                ItemCount = o.Items.Sum(i => i.Quantity)
            })
            .ToListAsync();

    public Task<OrderDetailDto?> GetAsync(int userId, int orderId) =>
        db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId)   // someone else's order looks like "not found"
            .Select(o => new OrderDetailDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
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
            })
            .FirstOrDefaultAsync();

    // ---------- Cancel ----------
    public Task CancelAsync(int userId, int orderId) => CancelCoreAsync(orderId, ownerUserId: userId, actorId: userId);

    public Task CancelAsAdminAsync(int adminId, int orderId) => CancelCoreAsync(orderId, ownerUserId: null, actorId: adminId);

    private async Task CancelCoreAsync(int orderId, int? ownerUserId, int actorId)
    {
        // a customer can only cancel their own order; for anyone else it simply "does not exist"
        var exists = await db.Orders.AnyAsync(o => o.Id == orderId && (ownerUserId == null || o.UserId == ownerUserId));
        if (!exists)
            throw new AppException("Order not found.", 404);

        // The status change and the stock return must succeed or fail together.
        await using var tx = await db.Database.BeginTransactionAsync();

        var now = (DateTime?)DateTime.UtcNow;
        var by = (int?)actorId;

        // COMPARE-AND-SET: "set Cancelled WHERE it is still Pending". If two people cancel at the
        // same moment, only one update changes a row, so the stock is returned only once.
        var rows = await db.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatuses.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, OrderStatuses.Cancelled)
                .SetProperty(o => o.UpdatedAt, now)
                .SetProperty(o => o.UpdatedBy, by));

        if (rows == 0)
            throw new AppException(
                "Only an order that is still Pending can be cancelled. A paid order needs a refund, which is not built yet.", 400);

        // give the coupon use back (the order never completed, so the coupon was not really spent)
        var couponId = await db.Orders.Where(o => o.Id == orderId).Select(o => o.CouponId).FirstAsync();
        if (couponId is not null)
        {
            await db.Set<Coupon>()
                .Where(c => c.Id == couponId && c.UsedCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1));
        }

        var items = await db.OrderItems
            .Where(i => i.OrderId == orderId)
            .Select(i => new { i.BookId, i.Quantity })
            .ToListAsync();

        foreach (var item in items)
        {
            var quantity = item.Quantity;

            // "Stock = Stock + quantity" is done inside SQL, so two changes at once can't overwrite each other
            await db.Books
                .Where(b => b.Id == item.BookId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, b => b.Stock + quantity));
        }

        await tx.CommitAsync();
    }
}

// The two columns the stored procedure returns
internal class PlaceOrderRow
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}
