using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class WishlistService(AppDbContext db) : IWishlistService
{
    public Task<List<WishlistItemDto>> ListAsync(int userId) =>
        db.Set<WishlistItem>().AsNoTracking()
            .Where(w => w.UserId == userId && !w.Book.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WishlistItemDto
            {
                BookId = w.BookId, Title = w.Book.Title, Author = w.Book.Author,
                Price = w.Book.Price, Stock = w.Book.Stock, ImageUrl = w.Book.ImageUrl, AddedAt = w.CreatedAt
            })
            .ToListAsync();

    public async Task AddAsync(int userId, int bookId)
    {
        if (!await db.Books.AnyAsync(b => b.Id == bookId && !b.IsDeleted))
            throw new AppException("Book not found.", 404);

        // already in the list? then nothing to do (idempotent: calling twice is safe)
        if (await db.Set<WishlistItem>().AnyAsync(w => w.UserId == userId && w.BookId == bookId))
            return;

        db.Add(new WishlistItem { UserId = userId, BookId = bookId });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // a parallel request inserted the same row first; the unique index protected us. Goal reached, so no error.
        }
    }

    public async Task RemoveAsync(int userId, int bookId) =>
        // hard delete is fine here: a wishlist row holds no history worth keeping
        await db.Set<WishlistItem>().Where(w => w.UserId == userId && w.BookId == bookId).ExecuteDeleteAsync();
}
