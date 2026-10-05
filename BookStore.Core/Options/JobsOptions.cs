namespace BookStore.Core.Options;

// Bound to the optional "Jobs" section. Every value has a default, so appsettings needs nothing.
public class JobsOptions
{
    public int EmailPollSeconds { get; set; } = 10;
    public int EmailBatchSize { get; set; } = 20;
    public int EmailMaxAttempts { get; set; } = 5;
    public int StuckSendingMinutes { get; set; } = 5;       // a "Sending" row older than this is given back
    public int CleanupEveryHours { get; set; } = 6;
    public int TokenRetentionDays { get; set; } = 30;
    public int SentEmailRetentionDays { get; set; } = 30;
    public int FailedEmailRetentionDays { get; set; } = 90;
}
