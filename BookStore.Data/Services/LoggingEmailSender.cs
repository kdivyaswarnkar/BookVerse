using BookStore.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookStore.Data.Services;

// Development sender: writes the email to the log instead of sending it.
// Handy: you can copy the verification / reset link straight from the log window.
// (Do not use in production: the body contains tokens.)
public class LoggingEmailSender(ILogger<LoggingEmailSender> log) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        log.LogInformation("EMAIL (not really sent) To: {To} | Subject: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
