using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class OrderService(AppDbContext db) : IOrderService
{
    // PLACING an order is done by the stored procedure, in ONE database transaction:
    // check address + cart, lock the books, check stock, create order and items,
    // reduce stock, empty the cart. If anything fails, nothing is saved.
    // Its business errors (50002 cart empty, 50003 no stock, 50005 bad address) are turned
    // into 400 responses by ExceptionMiddleware.
    public async Task<PlaceOrderResponse> PlaceOrderAsync(int userId, int addressId)
    {
        var rows = await db.Database
            .SqlQuery<PlaceOrderRow>($"EXEC sales.sp_PlaceOrder {userId}, {addressId}")   // values are sent as parameters
            .ToListAsync();

        var row = rows.Single();
        return new PlaceOrderResponse(row.OrderId, row.TotalAmount, "Pending");
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
}

// The two columns the stored procedure returns
internal class PlaceOrderRow
{
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}