namespace BookStore.Core.Entities;

// One email waiting to be sent. A background worker (EmailSenderWorker) sends them.
public class EmailQueueItem
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "Pending";   // Pending, Sending, Sent, Failed
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }      // retry not before this time
    public DateTime? LockedAt { get; set; }           // when a worker took it
}
