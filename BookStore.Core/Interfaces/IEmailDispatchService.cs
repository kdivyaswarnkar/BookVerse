using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IEmailDispatchService
{
    Task<EmailBatchResult> ProcessBatchAsync(CancellationToken ct);              // called by the worker (and by the admin "run now")
    Task<PagedResult<EmailQueueListItemDto>> ListAsync(string? status, int page, int pageSize);
    Task RetryAsync(int id);                                                      // admin: put a Failed email back in the queue
}
