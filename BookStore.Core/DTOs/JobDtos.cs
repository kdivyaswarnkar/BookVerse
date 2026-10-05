namespace BookStore.Core.DTOs;

public record EmailBatchResult(int Sent, int Retrying, int Failed, int Recovered);

public record CleanupResult(int RefreshTokensDeleted, int ResetTokensCleared, int SentEmailsDeleted, int FailedEmailsDeleted);

public class EmailQueueListItemDto
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Status { get; set; } = "";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
}
