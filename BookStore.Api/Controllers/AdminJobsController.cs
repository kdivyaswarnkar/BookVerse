using BookStore.Core.Constants;
using BookStore.Core.DTOs;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers;

// Admin window into the background work: look at the email queue and run a job right now (handy for testing).
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminJobsController(IEmailDispatchService emails, ICleanupService cleanup) : ControllerBase
{
    private static readonly string[] Statuses = ["Pending", "Sending", "Sent", "Failed"];

    // GET api/admin/emails?status=Failed&page=1&pageSize=20
    [HttpGet("emails")]
    public async Task<ActionResult<PagedResult<EmailQueueListItemDto>>> List(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(status) && !Statuses.Contains(status))
            throw new AppException($"Status must be one of: {string.Join(", ", Statuses)}.", 400);

        return Ok(await emails.ListAsync(status, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100)));
    }

    // POST api/admin/emails/5/retry
    [HttpPost("emails/{id:int}/retry")]
    public async Task<IActionResult> Retry(int id)
    {
        await emails.RetryAsync(id);
        return NoContent();
    }

    // POST api/admin/jobs/send-emails-now
    [HttpPost("jobs/send-emails-now")]
    public async Task<ActionResult<EmailBatchResult>> SendNow(CancellationToken ct) =>
        Ok(await emails.ProcessBatchAsync(ct));

    // POST api/admin/jobs/cleanup-now
    [HttpPost("jobs/cleanup-now")]
    public async Task<ActionResult<CleanupResult>> CleanupNow(CancellationToken ct) =>
        Ok(await cleanup.RunAsync(ct));
}
