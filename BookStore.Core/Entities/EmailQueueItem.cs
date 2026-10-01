namespace BookStore.Core.Entities;

// One email waiting to be sent (a background service sends them in BE-12)
public class EmailQueueItem
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "Pending";   // Pending, Sent, Failed
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}