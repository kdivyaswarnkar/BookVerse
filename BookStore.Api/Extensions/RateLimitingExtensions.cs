using System.Security.Claims;
using System.Threading.RateLimiting;
using BookStore.Core.Constants;
using BookStore.Core.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookStore.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var o = config.GetSection("RateLimiting").Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // What the caller sees when blocked: 429 + a Retry-After header + a JSON message.
            options.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                var seconds = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                http.Response.Headers.RetryAfter = seconds.ToString();
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimit")
                    .LogWarning("Rate limit hit: {Method} {Path} from {Ip}", http.Request.Method, http.Request.Path, ClientIp(http));

                await http.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429,
                    Title = "Too many requests",
                    Detail = $"Too many requests. Please try again in {seconds} seconds."
                }, ct);
            };

            // 1. SAFETY NET for the whole API: a fixed window per IP address.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                !o.Enabled
                    ? RateLimitPartition.GetNoLimiter("off")
                    : RateLimitPartition.GetFixedWindowLimiter($"global:{ClientIp(ctx)}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = o.GlobalPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // 2. LOGIN-type endpoints: sliding window per IP (smoother than a fixed window, no burst at the window edge).
            options.AddPolicy(RateLimitPolicies.Login, ctx =>
                !o.Enabled
                    ? RateLimitPartition.GetNoLimiter("off")
                    : RateLimitPartition.GetSlidingWindowLimiter($"login:{ClientIp(ctx)}", _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = o.LoginPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0
                    }));

            // 3. EMAIL-sending endpoints: stricter, because every call sends an email.
            options.AddPolicy(RateLimitPolicies.Email, ctx =>
                !o.Enabled
                    ? RateLimitPartition.GetNoLimiter("off")
                    : RateLimitPartition.GetSlidingWindowLimiter($"email:{ClientIp(ctx)}", _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = o.EmailPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0
                    }));

            // 4. CHECKOUT-type endpoints: token bucket PER USER (a short burst is fine, then a steady rate).
            options.AddPolicy(RateLimitPolicies.Checkout, ctx =>
                !o.Enabled
                    ? RateLimitPartition.GetNoLimiter("off")
                    : RateLimitPartition.GetTokenBucketLimiter($"checkout:{UserOrIp(ctx)}", _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = o.CheckoutBurst,
                        TokensPerPeriod = Math.Max(1, o.CheckoutPerMinute / 6),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string ClientIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // logged-in users are counted by user id (not shared with others behind the same office IP)
    private static string UserOrIp(HttpContext ctx) =>
        ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } id ? $"u{id}" : ClientIp(ctx);
}
