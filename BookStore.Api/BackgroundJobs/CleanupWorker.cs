using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.Extensions.Options;

namespace BookStore.Api.BackgroundJobs;

// Runs the housekeeping once shortly after start, then every few hours.
public class CleanupWorker(
    IServiceScopeFactory scopes, IOptions<JobsOptions> options, ILogger<CleanupWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);   // let the app finish starting first

            using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.CleanupEveryHours));
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ICleanupService>().RunAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Cleanup round failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // normal: the app is shutting down
        }
    }
}
