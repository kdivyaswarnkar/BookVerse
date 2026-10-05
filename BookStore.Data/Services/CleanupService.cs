using BookStore.Core.DTOs;
using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Data.Services;

// Housekeeping: old data that nobody needs any more. Every step is one set-based SQL statement.
public class CleanupService(AppDbContext db, IOptions<JobsOptions> jobs, ILogger<CleanupService> log) : ICleanupService
{
    public async Task<CleanupResult> RunAsync(CancellationToken ct)
    {
        var o = jobs.Value;
        var now = DateTime.UtcNow;
        var tokenCutoff = now.AddDays(-o.TokenRetentionDays);

        // refresh tokens that expired or were revoked a long time ago (kept a while for audit / reuse detection)
        var tokens = await db.RefreshTokens
            .Where(t => t.ExpiresAt < tokenCutoff || (t.RevokedAt != null && t.RevokedAt < tokenCutoff))
            .ExecuteDeleteAsync(ct);

        // password-reset tokens that already expired: erase them (the account row stays)
        var resets = await db.Users
            .Where(u => u.ResetToken != null && u.ResetTokenExpiry < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.ResetToken, (string?)null)
                .SetProperty(u => u.ResetTokenExpiry, (DateTime?)null), ct);

        var sentCutoff = now.AddDays(-o.SentEmailRetentionDays);
        var sentEmails = await db.EmailQueue
            .Where(e => e.Status == "Sent" && e.SentAt < sentCutoff)
            .ExecuteDeleteAsync(ct);

        var failedCutoff = now.AddDays(-o.FailedEmailRetentionDays);
        var failedEmails = await db.EmailQueue
            .Where(e => e.Status == "Failed" && e.CreatedAt < failedCutoff)
            .ExecuteDeleteAsync(ct);

        var result = new CleanupResult(tokens, resets, sentEmails, failedEmails);
        log.LogInformation("Cleanup done: {@Result}", result);
        return result;
    }
}
