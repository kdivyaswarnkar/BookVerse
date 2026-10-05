using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IWishlistService
{
    Task<List<WishlistItemDto>> ListAsync(int userId);
    Task AddAsync(int userId, int bookId);      // idempotent
    Task RemoveAsync(int userId, int bookId);   // idempotent
}
