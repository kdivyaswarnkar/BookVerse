using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BookStore.Data.Repositories;

// DECORATOR pattern: same interface as BookRepository, wraps it and adds caching.
// The controller does not know the cache exists.
public class CachedBookRepository(BookRepository inner, IMemoryCache cache) : IBookRepository
{
    private const string CategoriesKey = "categories";
    private static readonly TimeSpan CategoriesLifetime = TimeSpan.FromMinutes(30);

    // CACHE-ASIDE: look in the cache first, go to the database only on a miss, then store the result.
    // Categories change rarely and are read on every page, so they are a good cache candidate.
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await cache.GetOrCreateAsync(CategoriesKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CategoriesLifetime;   // never older than 30 minutes
            return inner.GetCategoriesAsync();
        });

        return categories!;
    }

    // Book lists and details are cached one level up, as whole HTTP responses (output caching),
    // so these two simply pass through.
    public Task<PagedResult<BookListItemDto>> SearchAsync(
        string? query, int? categoryId, int page, int pageSize) =>
        inner.SearchAsync(query, categoryId, page, pageSize);

    public Task<BookDetailDto?> GetByIdAsync(int id) => inner.GetByIdAsync(id);
}