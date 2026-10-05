using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Data.Services;
using BookStore.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests.Integration;

public class CouponCheckoutTests
{
    [SqlServerFact]
    public async Task PlaceOrder_WithPercentCoupon_AppliesDiscount_AndCountsTheUse()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 100m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 2);   // subtotal 200
        var coupon = await TestData.CreateCouponAsync(db, "Percent", 10);

        var result = await new OrderService(db).PlaceOrderAsync(user.Id, address.Id, coupon.Code);

        Assert.Equal(180m, result.TotalAmount);

        await using var check = TestDb.NewContext();
        var order = await check.Orders.SingleAsync(o => o.Id == result.OrderId);
        Assert.Equal(20m, order.DiscountAmount);
        Assert.Equal(coupon.Id, order.CouponId);
        Assert.Equal(1, await check.Coupons.Where(c => c.Id == coupon.Id).Select(c => c.UsedCount).SingleAsync());
    }

    [SqlServerFact]
    public async Task PlaceOrder_WithFlatCoupon_BiggerThanTheOrder_NeverGoesNegative()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 30m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 1);
        var coupon = await TestData.CreateCouponAsync(db, "Flat", 50);

        var result = await new OrderService(db).PlaceOrderAsync(user.Id, address.Id, coupon.Code);

        Assert.Equal(0m, result.TotalAmount);
    }

    [SqlServerFact]
    public async Task PlaceOrder_WithBadCoupon_IsRefused_AndTheWholeOrderRollsBack()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 100m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 1);
        var expired = await TestData.CreateCouponAsync(db, validFromDays: -10, validToDays: -5);

        var ex = await Assert.ThrowsAnyAsync<SqlException>(() => new OrderService(db).PlaceOrderAsync(user.Id, address.Id, expired.Code));
        Assert.Equal(50006, ex.Number);

        await using var check = TestDb.NewContext();
        Assert.Equal(5, await check.Books.Where(b => b.Id == book.Id).Select(b => b.Stock).SingleAsync());
        Assert.True(await check.CartItems.AnyAsync(c => c.UserId == user.Id));
        Assert.False(await check.Orders.AnyAsync(o => o.UserId == user.Id));
    }

    [SqlServerFact]
    public async Task PlaceOrder_BelowTheCouponMinimum_IsRefused()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 100m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 1);
        var coupon = await TestData.CreateCouponAsync(db, minOrder: 500);

        var ex = await Assert.ThrowsAnyAsync<SqlException>(() => new OrderService(db).PlaceOrderAsync(user.Id, address.Id, coupon.Code));
        Assert.Equal(50006, ex.Number);
    }

    [SqlServerFact]
    public async Task LastCouponUse_TwoBuyersAtOnce_OnlyOneGetsIt()
    {
        await using var seed = TestDb.NewContext();
        var book1 = await TestData.CreateBookAsync(seed, stock: 5, price: 100m);
        var book2 = await TestData.CreateBookAsync(seed, stock: 5, price: 100m);   // different books: the race is about the COUPON
        var (u1, a1) = await TestData.CreateShopperAsync(seed, book1, 1);
        var (u2, a2) = await TestData.CreateShopperAsync(seed, book2, 1);
        var coupon = await TestData.CreateCouponAsync(seed, maxUses: 1);

        async Task<bool> Try(int userId, int addressId)
        {
            await using var db = TestDb.NewContext();
            try { await new OrderService(db).PlaceOrderAsync(userId, addressId, coupon.Code); return true; }
            catch (SqlException ex) when (ex.Number == 50006) { return false; }
        }

        var results = await Task.WhenAll(Try(u1.Id, a1.Id), Try(u2.Id, a2.Id));

        Assert.Equal(1, results.Count(r => r));
        await using var check = TestDb.NewContext();
        Assert.Equal(1, await check.Coupons.Where(c => c.Id == coupon.Id).Select(c => c.UsedCount).SingleAsync());
        Assert.Equal(1, await check.Orders.CountAsync(o => o.CouponId == coupon.Id));
    }

    [SqlServerFact]
    public async Task Preview_ShowsThePrice_WithoutUsingTheCoupon()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 100m);
        var (user, _) = await TestData.CreateShopperAsync(db, book, quantity: 2);
        var coupon = await TestData.CreateCouponAsync(db, "Percent", 25);

        var preview = await new CouponService(db).PreviewAsync(user.Id, coupon.Code);

        Assert.Equal(200m, preview.Subtotal);
        Assert.Equal(50m, preview.Discount);
        Assert.Equal(150m, preview.Total);

        await using var check = TestDb.NewContext();
        Assert.Equal(0, await check.Coupons.Where(c => c.Id == coupon.Id).Select(c => c.UsedCount).SingleAsync());
    }

    [SqlServerFact]
    public async Task Create_DuplicateCode_GivesConflict()
    {
        await using var db = TestDb.NewContext();
        var code = "DUP" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var request = new CreateCouponRequest
        {
            Code = code, DiscountType = "Percent", DiscountValue = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(1)
        };

        await new CouponService(db).CreateAsync(request);

        await using var db2 = TestDb.NewContext();
        var ex = await Assert.ThrowsAsync<AppException>(() => new CouponService(db2).CreateAsync(request));
        Assert.Equal(409, ex.StatusCode);
    }
}
