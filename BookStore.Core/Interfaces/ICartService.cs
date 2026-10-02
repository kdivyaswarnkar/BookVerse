using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<CartDto> AddItemAsync(int userId, AddToCartRequest request);
    Task<CartDto> UpdateItemAsync(int userId, int bookId, int quantity);
    Task RemoveItemAsync(int userId, int bookId);
    Task ClearAsync(int userId);
}