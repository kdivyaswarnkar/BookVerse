using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Repositories;

public class BookRepository(AppDbContext db) : IBookRepository
{
    public async Task<PagedResult<BookListItemDto>> SearchAsync(
        string? query, int? categoryId, int page, int pageSize)
    {
        var books = db.Books.AsNoTracking().Where(b => !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
            books = books.Where(b => b.Title.Contains(query) || b.Author.Contains(query));

        if (categoryId is not null)
            books = books.Where(b => b.CategoryId == categoryId);

        var total = await books.CountAsync();

        var items = await books
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookListItemDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Price = b.Price,
                Stock = b.Stock,
                ImageUrl = b.ImageUrl,
                CategoryName = b.Category.Name
            })
            .ToListAsync();

        return new PagedResult<BookListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<BookDetailDto?> GetByIdAsync(int id) =>
        db.Books.AsNoTracking()
            .Where(b => b.Id == id && !b.IsDeleted)
            .Select(b => new BookDetailDto
            {
                Id = b.Id,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name,
                Title = b.Title,
                Author = b.Author,
                Isbn = b.Isbn,
                Description = b.Description,
                Price = b.Price,
                Stock = b.Stock,
                ImageUrl = b.ImageUrl
            })
            .FirstOrDefaultAsync();

    public Task<List<CategoryDto>> GetCategoriesAsync() =>
        db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
}