using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Data.Services;

public class EmailDispatchService(
    AppDbContext db, IEmailSender sender, IOptions<JobsOptions> jobs, ILogger<EmailDispatchService> log) : IEmailDispatchService
{
    public async Task<EmailBatchResult> ProcessBatchAsync(CancellationToken ct)
    {
        var o = jobs.Value;
        var now = DateTime.UtcNow;

        // 1. Rows stuck in "Sending" (the app stopped mid-send) go back to the queue.
        var stuckBefore = now.AddMinutes(-o.StuckSendingMinutes);
        var recovered = await db.EmailQueue
            .Where(e => e.Status == "Sending" && e.LockedAt < stuckBefore)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, "Pending").SetProperty(e => e.LockedAt, (DateTime?)null), ct);

        // 2. Which emails are due?
        var dueIds = await db.EmailQueue.AsNoTracking()
            .Where(e => e.Status == "Pending" && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Select(e => e.Id)
            .Take(o.EmailBatchSize)
            .ToListAsync(ct);

        int sent = 0, retrying = 0, failed = 0;

        foreach (var id in dueIds)
        {
            // 3. CLAIM it: "Pending -> Sending" only if it is still Pending. If another app instance took it
            //    a moment ago, 0 rows change and we skip it, so one email is never sent twice.
            var lockedAt = (DateTime?)DateTime.UtcNow;
            var claimed = await db.EmailQueue
                .Where(e => e.Id == id && e.Status == "Pending")
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, "Sending").SetProperty(e => e.LockedAt, lockedAt), ct);
            if (claimed == 0) continue;

            var mail = await db.EmailQueue.AsNoTracking().FirstAsync(e => e.Id == id, ct);

            try
            {
                await sender.SendAsync(mail.ToEmail, mail.Subject, mail.Body, ct);

                var sentAt = (DateTime?)DateTime.UtcNow;
                await db.EmailQueue.Where(e => e.Id == id).ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, "Sent")
                    .SetProperty(e => e.SentAt, sentAt)
                    .SetProperty(e => e.Attempts, mail.Attempts + 1)
                    .SetProperty(e => e.LastError, (string?)null)
                    .SetProperty(e => e.LockedAt, (DateTime?)null), ct);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var attempts = mail.Attempts + 1;
                var giveUp = attempts >= o.EmailMaxAttempts;
                var status = giveUp ? "Failed" : "Pending";
                // exponential backoff: 1, 2, 4, 8 minutes ...
                var next = giveUp ? (DateTime?)null : DateTime.UtcNow.AddMinutes(Math.Pow(2, attempts - 1));
                var error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;

                await db.EmailQueue.Where(e => e.Id == id).ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, status)
                    .SetProperty(e => e.Attempts, attempts)
                    .SetProperty(e => e.LastError, error)
                    .SetProperty(e => e.NextAttemptAt, next)
                    .SetProperty(e => e.LockedAt, (DateTime?)null), ct);

                log.LogWarning(ex, "Email {Id} failed (attempt {Attempts}/{Max})", id, attempts, o.EmailMaxAttempts);
                if (giveUp) failed++; else retrying++;
            }
        }

        if (sent + retrying + failed + recovered > 0)
            log.LogInformation("Email batch: {Sent} sent, {Retrying} will retry, {Failed} failed, {Recovered} recovered", sent, retrying, failed, recovered);

        return new EmailBatchResult(sent, retrying, failed, recovered);
    }

    public async Task<PagedResult<EmailQueueListItemDto>> ListAsync(string? status, int page, int pageSize)
    {
        var query = db.EmailQueue.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmailQueueListItemDto
            {
                Id = e.Id, ToEmail = e.ToEmail, Subject = e.Subject, Status = e.Status, Attempts = e.Attempts,
                LastError = e.LastError, CreatedAt = e.CreatedAt, SentAt = e.SentAt, NextAttemptAt = e.NextAttemptAt
            })
            .ToListAsync();

        return new PagedResult<EmailQueueListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task RetryAsync(int id)
    {
        var rows = await db.EmailQueue
            .Where(e => e.Id == id && e.Status == "Failed")
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Pending")
                .SetProperty(e => e.Attempts, 0)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null));

        if (rows == 0) throw new AppException("Only a Failed email can be retried (or it was not found).", 404);
    }
}
