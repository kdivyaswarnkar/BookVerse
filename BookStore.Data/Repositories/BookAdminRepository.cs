using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Repositories;

public class BookAdminRepository(AppDbContext db) : IBookAdminRepository
{
    public async Task<int> AddAsync(BookUpsertRequest request, int adminId)
    {
        await EnsureCategoryExistsAsync(request.CategoryId);

        var book = new Book { CreatedBy = adminId };
        Apply(book, request);

        db.Books.Add(book);
        await db.SaveChangesAsync();
        return book.Id;
    }

    public async Task<bool> UpdateAsync(int id, BookUpsertRequest request, int adminId)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        if (book is null) return false;

        await EnsureCategoryExistsAsync(request.CategoryId);

        Apply(book, request);
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdatedBy = adminId;

        // if the price changed, the database trigger writes a row to BookPriceHistory
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int adminId)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        if (book is null) return false;

        book.IsDeleted = true;   // soft delete: old orders keep pointing to this book
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdatedBy = adminId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetImageAsync(int id, string? imageUrl, int adminId)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        if (book is null) return false;

        book.ImageUrl = imageUrl;
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdatedBy = adminId;
        await db.SaveChangesAsync();
        return true;
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId))
            throw new AppException("Category not found.", 400);
    }

    private static void Apply(Book book, BookUpsertRequest r)
    {
        book.CategoryId = r.CategoryId;
        book.Title = r.Title.Trim();
        book.Author = r.Author.Trim();
        book.Isbn = string.IsNullOrWhiteSpace(r.Isbn) ? null : r.Isbn.Trim();
        book.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
        book.Price = r.Price;
        book.Stock = r.Stock;
    }
}