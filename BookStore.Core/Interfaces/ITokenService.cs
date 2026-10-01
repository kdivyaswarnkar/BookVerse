using BookStore.Core.Entities;

namespace BookStore.Core.Interfaces;

public interface ITokenService
{
    // Short-lived JWT the client sends with every request
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    // Long random value the client keeps to get new access tokens
    string CreateRefreshToken();

    // What we store in the database instead of the real refresh token
    string HashToken(string token);
}