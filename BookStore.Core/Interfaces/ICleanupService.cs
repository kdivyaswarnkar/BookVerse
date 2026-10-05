using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface ICleanupService
{
    Task<CleanupResult> RunAsync(CancellationToken ct);
}
