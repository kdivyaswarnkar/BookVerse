using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class CartService(AppDbContext db) : ICartService
{
    // ---------- Read ----------
    public async Task<CartDto> GetCartAsync(int userId)
    {
        // one query, one JOIN: the price and stock shown are always the current ones
        var items = await db.CartItems
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.Book.IsDeleted)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(c => new CartItemDto
            {
                BookId = c.BookId,
                Title = c.Book.Title,
                Author = c.Book.Author,
                ImageUrl = c.Book.ImageUrl,
                Price = c.Book.Price,
                Quantity = c.Quantity,
                LineTotal = c.Book.Price * c.Quantity,
                Stock = c.Book.Stock,
                IsAvailable = c.Book.Stock >= c.Quantity
            })
            .ToListAsync();

        return new CartDto { Items = items };
    }

    // ---------- Add (a second add of the same book increases the quantity) ----------
    public async Task<CartDto> AddItemAsync(int userId, AddToCartRequest request)
    {
        var stock = await db.Books
            .AsNoTracking()
            .Where(b => b.Id == request.BookId && !b.IsDeleted)
            .Select(b => (int?)b.Stock)
            .FirstOrDefaultAsync();

        if (stock is null)
            throw new AppException("Book not found.", 404);

        var item = await db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == request.BookId);

        var newQuantity = (item?.Quantity ?? 0) + request.Quantity;
        EnsureQuantityAllowed(newQuantity, stock.Value);

        if (item is null)
        {
            db.CartItems.Add(new CartItem { UserId = userId, BookId = request.BookId, Quantity = newQuantity });
        }
        else
        {
            item.Quantity = newQuantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await SaveAsync();
        return await GetCartAsync(userId);
    }

    // ---------- Change the quantity ----------
    public async Task<CartDto> UpdateItemAsync(int userId, int bookId, int quantity)
    {
        var item = await db.CartItems
            .Include(c => c.Book)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId && !c.Book.IsDeleted);

        if (item is null)
            throw new AppException("This book is not in your cart.", 404);

        EnsureQuantityAllowed(quantity, item.Book.Stock);

        item.Quantity = quantity;
        item.UpdatedAt = DateTime.UtcNow;

        await SaveAsync();
        return await GetCartAsync(userId);
    }

    // ---------- Remove and clear (deleting something already gone is fine) ----------
    public async Task RemoveItemAsync(int userId, int bookId) =>
        await db.CartItems
            .Where(c => c.UserId == userId && c.BookId == bookId)
            .ExecuteDeleteAsync();

    public async Task ClearAsync(int userId) =>
        await db.CartItems
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync();

    // ---------- helpers ----------
    // The rules that depend on the current stock. (Range checks on the request itself
    // are done earlier, by the data annotations on the DTO.)
    private static void EnsureQuantityAllowed(int quantity, int stock)
    {
        if (stock <= 0)
            throw new AppException("This book is out of stock.", 400);

        if (quantity > CartRules.MaxQuantityPerItem)
            throw new AppException($"You can buy at most {CartRules.MaxQuantityPerItem} copies of one book.", 400);

        if (quantity > stock)
            throw new AppException($"Only {stock} {(stock == 1 ? "copy is" : "copies are")} available.", 400);
    }

    private async Task SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // two requests added the same book at the same moment
            throw new AppException("Your cart changed at the same time. Please try again.", 409);
        }
    }
}