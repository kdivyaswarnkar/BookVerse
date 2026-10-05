using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class ReviewService(AppDbContext db) : IReviewService
{
    public async Task<BookReviewsDto> ListAsync(int bookId, int page, int pageSize)
    {
        await EnsureBookExistsAsync(bookId);

        var query = db.Set<Review>().AsNoTracking().Where(r => r.BookId == bookId);   // soft-deleted rows are filtered out

        // summary computed in SQL (one aggregate query), not by loading every review
        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Avg = g.Average(r => (double)r.Rating) })
            .FirstOrDefaultAsync();

        var total = summary?.Count ?? 0;

        var items = await (
            from r in query
            join u in db.Users on r.UserId equals u.Id
            orderby r.CreatedAt descending, r.Id descending
            select new ReviewDto
            {
                Id = r.Id,
                Reviewer = u.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                // correlated EXISTS: did this reviewer pay for an order containing the book?
                VerifiedPurchase = db.OrderItems.Any(oi =>
                    oi.BookId == bookId &&
                    db.Orders.Any(o => o.Id == oi.OrderId && o.UserId == r.UserId &&
                        (o.Status == OrderStatuses.Paid || o.Status == OrderStatuses.Shipped ||
                         o.Status == OrderStatuses.Delivered)))
            })
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new BookReviewsDto
        {
            AverageRating = summary is null ? 0 : Math.Round((decimal)summary.Avg, 2),
            ReviewCount = total,
            Reviews = new PagedResult<ReviewDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total }
        };
    }

    public async Task<ReviewDto> SaveAsync(int userId, int bookId, SaveReviewRequest request)
    {
        await EnsureBookExistsAsync(bookId);

        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        // IgnoreQueryFilters: also find my soft-deleted review. The unique index allows only ONE row per
        // (book, user), so "review again after deleting" must revive that row, not insert a new one.
        var review = await db.Set<Review>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);

        if (review is null)
        {
            review = new Review { BookId = bookId, UserId = userId };
            db.Add(review);
        }
        else
        {
            review.UpdatedAt = DateTime.UtcNow;
        }

        review.Rating = (byte)request.Rating;
        review.Comment = comment;
        review.IsDeleted = false;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // two quick requests both tried to insert: the unique index stopped the second one
            throw new AppException("Your review is already being saved. Please try again.", 409);
        }

        var name = await db.Users.Where(u => u.Id == userId).Select(u => u.Name).FirstAsync();
        var verified = await db.OrderItems.AnyAsync(oi => oi.BookId == bookId &&
            db.Orders.Any(o => o.Id == oi.OrderId && o.UserId == userId &&
                (o.Status == OrderStatuses.Paid || o.Status == OrderStatuses.Shipped || o.Status == OrderStatuses.Delivered)));

        return new ReviewDto
        {
            Id = review.Id, Reviewer = name, Rating = review.Rating, Comment = review.Comment,
            CreatedAt = review.CreatedAt, VerifiedPurchase = verified
        };
    }

    public async Task DeleteMineAsync(int userId, int bookId)
    {
        // soft delete in one statement; 0 rows means there was nothing of mine to delete
        var rows = await db.Set<Review>()
            .Where(r => r.BookId == bookId && r.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

        if (rows == 0) throw new AppException("You have no review for this book.", 404);
    }

    public async Task DeleteAsAdminAsync(int reviewId)
    {
        var rows = await db.Set<Review>()
            .Where(r => r.Id == reviewId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

        if (rows == 0) throw new AppException("Review not found.", 404);
    }

    private async Task EnsureBookExistsAsync(int bookId)
    {
        if (!await db.Books.AnyAsync(b => b.Id == bookId && !b.IsDeleted))
            throw new AppException("Book not found.", 404);
    }
}
