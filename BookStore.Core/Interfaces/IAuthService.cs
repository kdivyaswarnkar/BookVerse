using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
}