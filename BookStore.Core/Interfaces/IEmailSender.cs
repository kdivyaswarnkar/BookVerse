namespace BookStore.Core.Interfaces;

// Sends ONE email now. Throws if it fails (the dispatcher decides about retries).
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
