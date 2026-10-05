using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Data.Services;
using BookStore.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests.Integration;

public class ReviewAndWishlistTests
{
    [SqlServerFact]
    public async Task Review_SavedTwice_IsEditedNotDuplicated()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, 5, 10m);
        var user = await TestData.CreateUserAsync(db);
        var reviews = new ReviewService(db);

        await reviews.SaveAsync(user.Id, book.Id, new SaveReviewRequest { Rating = 5, Comment = "Loved it" });
        await reviews.SaveAsync(user.Id, book.Id, new SaveReviewRequest { Rating = 3, Comment = "On second thought" });

        await using var check = TestDb.NewContext();
        var rows = await check.Set<Review>().Where(r => r.BookId == book.Id).ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal(3, row.Rating);
    }

    [SqlServerFact]
    public async Task Review_DeletedThenWrittenAgain_RevivesTheSameRow()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, 5, 10m);
        var user = await TestData.CreateUserAsync(db);
        var reviews = new ReviewService(db);

        var first = await reviews.SaveAsync(user.Id, book.Id, new SaveReviewRequest { Rating = 4 });
        await reviews.DeleteMineAsync(user.Id, book.Id);
        Assert.Equal(0, (await reviews.ListAsync(book.Id, 1, 10)).ReviewCount);   // hidden from readers

        var again = await reviews.SaveAsync(user.Id, book.Id, new SaveReviewRequest { Rating = 2 });

        Assert.Equal(first.Id, again.Id);                                          // same row, not a new one
        await using var check = TestDb.NewContext();
        Assert.Equal(1, await check.Set<Review>().IgnoreQueryFilters().CountAsync(r => r.BookId == book.Id));
    }

    [SqlServerFact]
    public async Task Review_Average_AndCount_AreCalculatedInSql()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, 5, 10m);
        var u1 = await TestData.CreateUserAsync(db);
        var u2 = await TestData.CreateUserAsync(db);
        var reviews = new ReviewService(db);

        await reviews.SaveAsync(u1.Id, book.Id, new SaveReviewRequest { Rating = 5 });
        await reviews.SaveAsync(u2.Id, book.Id, new SaveReviewRequest { Rating = 4 });

        var list = await reviews.ListAsync(book.Id, 1, 10);

        Assert.Equal(2, list.ReviewCount);
        Assert.Equal(4.5m, list.AverageRating);
    }

    [SqlServerFact]
    public async Task Review_VerifiedPurchase_IsOnlyTrueForPaidBuyers()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, 5, 10m);
        var (buyer, address) = await TestData.CreateShopperAsync(db, book, 1);
        var bystander = await TestData.CreateUserAsync(db);
        var placed = await new OrderService(db).PlaceOrderAsync(buyer.Id, address.Id);

        var providerPaymentId = "pay_" + Guid.NewGuid().ToString("N");
        db.Payments.Add(new Payment { OrderId = placed.OrderId, Provider = "Fake", ProviderPaymentId = providerPaymentId,
            Amount = placed.TotalAmount, Status = "Created", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sales.sp_MarkPaymentSuccess {providerPaymentId}");

        var reviews = new ReviewService(db);
        var buyersReview = await reviews.SaveAsync(buyer.Id, book.Id, new SaveReviewRequest { Rating = 5 });
        var bystandersReview = await reviews.SaveAsync(bystander.Id, book.Id, new SaveReviewRequest { Rating = 1 });

        Assert.True(buyersReview.VerifiedPurchase);
        Assert.False(bystandersReview.VerifiedPurchase);
    }

    [SqlServerFact]
    public async Task Review_ForAMissingBook_IsNotFound()
    {
        await using var db = TestDb.NewContext();
        var user = await TestData.CreateUserAsync(db);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            new ReviewService(db).SaveAsync(user.Id, 2_000_000_000, new SaveReviewRequest { Rating = 5 }));

        Assert.Equal(404, ex.StatusCode);
    }

    [SqlServerFact]
    public async Task Wishlist_AddTwice_KeepsOneRow_RemoveTwice_DoesNotFail()
    {
        await using var db = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(db, 5, 10m);
        var user = await TestData.CreateUserAsync(db);
        var wishlist = new WishlistService(db);

        await wishlist.AddAsync(user.Id, book.Id);
        await wishlist.AddAsync(user.Id, book.Id);
        Assert.Single(await wishlist.ListAsync(user.Id));

        await wishlist.RemoveAsync(user.Id, book.Id);
        await wishlist.RemoveAsync(user.Id, book.Id);
        Assert.Empty(await wishlist.ListAsync(user.Id));
    }

    [SqlServerFact]
    public async Task Wishlist_TwoAddsAtTheSameTime_StillOneRow_AndNoError()
    {
        await using var seed = TestDb.NewContext();
        var book = await TestData.CreateBookAsync(seed, 5, 10m);
        var user = await TestData.CreateUserAsync(seed);

        async Task Add()
        {
            await using var db = TestDb.NewContext();
            await new WishlistService(db).AddAsync(user.Id, book.Id);
        }

        await Task.WhenAll(Add(), Add());   // the unique index decides; the loser must not throw

        await using var check = TestDb.NewContext();
        Assert.Equal(1, await check.Set<WishlistItem>().CountAsync(w => w.UserId == user.Id && w.BookId == book.Id));
    }
}
