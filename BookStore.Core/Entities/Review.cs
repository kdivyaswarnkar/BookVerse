namespace BookStore.Core.Entities;

public class Review
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int UserId { get; set; }
    public byte Rating { get; set; }          // 1 to 5 (TINYINT in SQL)
    public string? Comment { get; set; }
    public bool IsDeleted { get; set; }       // soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
