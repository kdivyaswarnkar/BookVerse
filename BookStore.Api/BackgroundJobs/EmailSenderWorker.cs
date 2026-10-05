using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.Extensions.Options;

namespace BookStore.Api.BackgroundJobs;

// Runs for the whole life of the app. Every few seconds it sends the emails waiting in ops.EmailQueue.
// Why a queue + worker: the register / forgot-password request returns at once, and a slow or broken
// mail server can never make a user wait or fail.
public class EmailSenderWorker(
    IServiceScopeFactory scopes, IOptions<JobsOptions> options, ILogger<EmailSenderWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("Email sender worker started");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.EmailPollSeconds));

        try
        {
            do
            {
                try
                {
                    // A worker lives forever (singleton) but DbContext is scoped, so every round makes its own scope.
                    using var scope = scopes.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IEmailDispatchService>();
                    await dispatcher.ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // never let one bad round kill the worker
                    log.LogError(ex, "Email sender round failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // normal: the app is shutting down
        }

        log.LogInformation("Email sender worker stopped");
    }
}
