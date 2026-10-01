using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

// Write side. Kept apart from IBookRepository (read side) so the read side can be cached freely.
public interface IBookAdminRepository
{
    Task<int> AddAsync(BookUpsertRequest request, int adminId);
    Task<bool> UpdateAsync(int id, BookUpsertRequest request, int adminId);
    Task<bool> DeleteAsync(int id, int adminId);                    // soft delete
    Task<bool> SetImageAsync(int id, string? imageUrl, int adminId);
}