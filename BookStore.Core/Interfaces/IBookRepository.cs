using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IBookRepository
{
    Task<PagedResult<BookListItemDto>> SearchAsync(string? query, int? categoryId, int page, int pageSize);
    Task<BookDetailDto?> GetByIdAsync(int id);
    Task<List<CategoryDto>> GetCategoriesAsync();
}