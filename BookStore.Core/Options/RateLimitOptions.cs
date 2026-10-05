namespace BookStore.Core.Options;

// Bound to the optional "RateLimiting" section of appsettings.json. Every value has a default.
public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;          // set false only for load tests
    public int GlobalPerMinute { get; set; } = 300;    // every endpoint, per IP address
    public int LoginPerMinute { get; set; } = 10;      // per IP address
    public int EmailPerMinute { get; set; } = 5;       // per IP address
    public int CheckoutBurst { get; set; } = 10;       // per user: how many calls can be made at once
    public int CheckoutPerMinute { get; set; } = 30;   // per user: steady rate after the burst
}
