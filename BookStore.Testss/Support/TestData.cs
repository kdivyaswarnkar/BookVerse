using BookStore.Core.Entities;
using BookStore.Data;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests.Support;

// Small helpers that create fresh, unique rows for each test, so tests never depend on each other.
public static class TestData
{
    public static async Task<User> CreateUserAsync(AppDbContext db)
    {
        var id = Guid.NewGuid().ToString("N");
        var user = new User
        {
            Name = "Test " + id[..6], Email = $"t{id}@test.local", PasswordHash = "not-a-real-hash",
            Role = "Customer", EmailVerified = true, CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<Book> CreateBookAsync(AppDbContext db, int stock, decimal price)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Test Category");
        if (category is null)
        {
            category = new Category { Name = "Test Category", CreatedAt = DateTime.UtcNow };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }

        var book = new Book
        {
            CategoryId = category.Id, Title = "Test Book " + Guid.NewGuid().ToString("N")[..8], Author = "Test Author",
            Price = price, Stock = stock, CreatedAt = DateTime.UtcNow
        };
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return book;
    }

    public static async Task<Address> CreateAddressAsync(AppDbContext db, int userId)
    {
        var address = new Address
        {
            UserId = userId, FullName = "Test User", Phone = "9999999999", Line1 = "1 Test Street",
            City = "Pune", State = "MH", PostalCode = "411001", CreatedAt = DateTime.UtcNow
        };
        db.Add(address);
        await db.SaveChangesAsync();
        return address;
    }

    public static async Task AddToCartAsync(AppDbContext db, int userId, int bookId, int quantity)
    {
        db.CartItems.Add(new CartItem { UserId = userId, BookId = bookId, Quantity = quantity, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    // A user with a delivery address and one book in the cart, ready to place an order.
    public static async Task<(User User, Address Address)> CreateShopperAsync(AppDbContext db, Book book, int quantity)
    {
        var user = await CreateUserAsync(db);
        var address = await CreateAddressAsync(db, user.Id);
        await AddToCartAsync(db, user.Id, book.Id, quantity);
        return (user, address);
    }

    public static async Task<Coupon> CreateCouponAsync(
        AppDbContext db, string type = "Percent", decimal value = 10, decimal minOrder = 0, int? maxUses = null,
        bool active = true, int validFromDays = -1, int validToDays = 1)
    {
        var coupon = new Coupon
        {
            Code = "T" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            DiscountType = type, DiscountValue = value, MinOrderAmount = minOrder, MaxUses = maxUses,
            ValidFrom = DateTime.UtcNow.AddDays(validFromDays), ValidTo = DateTime.UtcNow.AddDays(validToDays),
            IsActive = active, CreatedAt = DateTime.UtcNow
        };
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync();
        return coupon;
    }
}
