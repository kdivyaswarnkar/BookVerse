namespace BookStore.Core.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // SHA-256 hash of the real token. The real token is only ever sent to the client.
    public string Token { get; set; } = "";

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}