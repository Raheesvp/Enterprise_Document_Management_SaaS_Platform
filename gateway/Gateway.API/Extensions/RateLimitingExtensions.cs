using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter — applies to all requests without a specific policy
            // If any user send more than 100 requests per minute, they get 429 Too Many Requests
            // Partition by IP address — each IP gets its own rate limit 100 requests/minute.
            //If exceeted that limit , they will block for 1 minute, others will not be affected
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(context =>
                {
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        });
                });

            //Auth-policy applies to auth endpoints — separate limits for login, token refresh etc.
            // If any user send more than 10 requests per minute, they get 429 Too Many Requests

            options.AddFixedWindowLimiter("auth-policy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });

            // 20 requests per minute for file uploads — separate policy for heavy operations
            // If any user send more than 20 requests per minute, they get 429 Too Many Requests

            options.AddFixedWindowLimiter("upload-policy", limiterOptions =>
            {
                limiterOptions.PermitLimit = 20;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; //if queue is fills up, they get the error.
                limiterOptions.QueueLimit = 5; // if user hits the limit ,instead of rejecting them, gateway put in queue .
            });

            // 429 Too Many Requests response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = 429;

                if (context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out var retry)
                        ? (int)retry.TotalSeconds
                        : 60
                }, cancellationToken);
            };
        });

        return services;
    }
}