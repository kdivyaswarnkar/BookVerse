using BookStore.Data.Services;
using BookStore.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests.Integration;

// These tests run the REAL stored procedure sp_PlaceOrder on the BookStore_Test database.
public class OrderPlacementTests
{
    // returns true if the order was placed, false if the database refused it for a business reason
    private static async Task<bool> TryPlaceAsync(int userId, int addressId, string? coupon = null)
    {
        await using var db = TestDb.NewContext();   // its own connection, like a separate web request
        try
        {
            await new OrderService(db).PlaceOrderAsync(userId, addressId, coupon);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 50003 or 50006)
        {
            return false;
        }
    }

    [SqlServerFact]
    public async Task PlaceOrder_ReducesStock_ClearsCart_AndFreezesThePrice()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 10, price: 100m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 3);

        var result = await new OrderService(db).PlaceOrderAsync(user.Id, address.Id);

        Assert.Equal(300m, result.TotalAmount);
        Assert.Equal("Pending", result.Status);

        await using var check = TestDb.NewContext();
        Assert.Equal(7, await check.Books.Where(b => b.Id == book.Id).Select(b => b.Stock).SingleAsync());
        Assert.False(await check.CartItems.AnyAsync(c => c.UserId == user.Id));
        Assert.Equal(100m, await check.OrderItems.Where(i => i.OrderId == result.OrderId).Select(i => i.UnitPrice).SingleAsync());
    }

    [SqlServerFact]
    public async Task PlaceOrder_NotEnoughStock_IsRefused_AndNothingChanges()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 2, price: 50m);
        var (user, address) = await TestData.CreateShopperAsync(db, book, quantity: 5);

        var ex = await Assert.ThrowsAnyAsync<SqlException>(() => new OrderService(db).PlaceOrderAsync(user.Id, address.Id));
        Assert.Equal(50003, ex.Number);

        await using var check = TestDb.NewContext();
        Assert.Equal(2, await check.Books.Where(b => b.Id == book.Id).Select(b => b.Stock).SingleAsync());   // stock untouched
        Assert.True(await check.CartItems.AnyAsync(c => c.UserId == user.Id));                                // cart kept
        Assert.False(await check.Orders.AnyAsync(o => o.UserId == user.Id));                                  // no order
    }

    [SqlServerFact]
    public async Task PlaceOrder_EmptyCart_IsRefused()
    {
        await using var db = TestDb.NewContext();
        var user = await TestData.CreateUserAsync(db);
        var address = await TestData.CreateAddressAsync(db, user.Id);

        var ex = await Assert.ThrowsAnyAsync<SqlException>(() => new OrderService(db).PlaceOrderAsync(user.Id, address.Id));
        Assert.Equal(50002, ex.Number);
    }

    [SqlServerFact]
    public async Task PlaceOrder_WithSomeoneElsesAddress_IsRefused()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, stock: 5, price: 10m);
        var (user, _) = await TestData.CreateShopperAsync(db, book, quantity: 1);
        var stranger = await TestData.CreateUserAsync(db);
        var strangersAddress = await TestData.CreateAddressAsync(db, stranger.Id);

        var ex = await Assert.ThrowsAnyAsync<SqlException>(() => new OrderService(db).PlaceOrderAsync(user.Id, strangersAddress.Id));
        Assert.Equal(50005, ex.Number);
    }

    [SqlServerFact]
    public async Task TwoBuyers_OneLastCopy_OnlyOneWins()
    {
        await using var seed = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(seed, stock: 1, price: 40m);
        var (u1, a1) = await TestData.CreateShopperAsync(seed, book, 1);
        var (u2, a2) = await TestData.CreateShopperAsync(seed, book, 1);

        // both click "Place order" at the same moment
        var results = await Task.WhenAll(TryPlaceAsync(u1.Id, a1.Id), TryPlaceAsync(u2.Id, a2.Id));

        Assert.Equal(1, results.Count(r => r));
        await using var check = TestDb.NewContext();
        Assert.Equal(0, await check.Books.Where(b => b.Id == book.Id).Select(b => b.Stock).SingleAsync());   // never negative
    }
}
