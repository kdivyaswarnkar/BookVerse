using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Data.Services;
using BookStore.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests.Integration;

public class OrderCancelTests
{
    [SqlServerFact]
    public async Task Cancel_ReturnsTheStock_AndTheCouponUse()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 10, price: 100m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 4);
        var coupon = await TestData.CreateCouponAsync(db, maxUses: 5);
        var orders = new OrderService(db);
        var placed = await orders.PlaceOrderAsync(user.Id, address.Id, coupon.Code);

        await orders.CancelAsync(user.Id, placed.OrderId);

        await using var check = TestDb.NewContext();
        Assert.Equal("Cancelled", await check.Orders.Where(o => o.Id == placed.OrderId).Select(o => o.Status).SingleAsync());
        Assert.Equal(10, await check.Books.Where(b => b.Id == book.Id).Select(b => b.Stock).SingleAsync());
        Assert.Equal(0, await check.Coupons.Where(c => c.Id == coupon.Id).Select(c => c.UsedCount).SingleAsync());
    }

    [SqlServerFact]
    public async Task Cancel_Twice_ReturnsStockOnlyOnce()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 10, price: 20m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 3);
        var orders = new OrderService(db);
        var placed = await orders.PlaceOrderAsync(user.Id, address.Id);

        await orders.CancelAsync(user.Id, placed.OrderId);
        var ex = await Assert.ThrowsAsync<AppException>(() => orders.CancelAsync(user.Id, placed.OrderId));

        Assert.Equal(400, ex.StatusCode);
        await using var check = TestDb.NewContext();
        Assert.Equal(10, await check.Books.Where(b => b.Id == book.Id).Select(b => b.Stock).SingleAsync());   // not 13
    }

    [SqlServerFact]
    public async Task Cancel_SomeoneElsesOrder_LooksLikeNotFound()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 20m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, 1);
        var stranger = await TestData.CreateUserAsync(db);
        var orders = new OrderService(db);
        var placed = await orders.PlaceOrderAsync(user.Id, address.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => orders.CancelAsync(stranger.Id, placed.OrderId));

        Assert.Equal(404, ex.StatusCode);
    }

    [SqlServerFact]
    public async Task PaymentArrivingAfterCancel_DoesNotReviveTheOrder()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 80m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, 1);
        var orders = new OrderService(db);
        var placed = await orders.PlaceOrderAsync(user.Id, address.Id);

        var providerPaymentId = "pay_" + Guid.NewGuid().ToString("N");
        db.Payments.Add(new Payment
        {
            OrderId = placed.OrderId, Provider = "Fake", ProviderPaymentId = providerPaymentId,
            Amount = placed.TotalAmount, Status = "Created", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await orders.CancelAsync(user.Id, placed.OrderId);
        // the provider's "payment succeeded" message arrives late
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sales.sp_MarkPaymentSuccess {providerPaymentId}");

        await using var check = TestDb.NewContext();
        Assert.Equal("Cancelled", await check.Orders.Where(o => o.Id == placed.OrderId).Select(o => o.Status).SingleAsync());
        Assert.Equal("Created", await check.Payments.Where(p => p.OrderId == placed.OrderId).Select(p => p.Status).SingleAsync());
    }

    [SqlServerFact]
    public async Task PaymentSuccess_OnAPendingOrder_MarksItPaid_AndRepeatingItIsHarmless()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 80m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, 1);
        var placed = await new OrderService(db).PlaceOrderAsync(user.Id, address.Id);

        var providerPaymentId = "pay_" + Guid.NewGuid().ToString("N");
        db.Payments.Add(new Payment
        {
            OrderId = placed.OrderId, Provider = "Fake", ProviderPaymentId = providerPaymentId,
            Amount = placed.TotalAmount, Status = "Created", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sales.sp_MarkPaymentSuccess {providerPaymentId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sales.sp_MarkPaymentSuccess {providerPaymentId}");   // webhook delivered twice

        await using var check = TestDb.NewContext();
        Assert.Equal("Paid", await check.Orders.Where(o => o.Id == placed.OrderId).Select(o => o.Status).SingleAsync());
    }
}
