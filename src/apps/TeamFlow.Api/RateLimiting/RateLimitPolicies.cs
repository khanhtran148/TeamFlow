using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace TeamFlow.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string Auth       = "auth";        // Login, Register, Refresh
    public const string Write      = "write";       // POST, PUT, DELETE
    public const string Search     = "search";      // Search endpoints
    public const string BulkAction = "bulk_action"; // Bulk operations
    public const string General    = "general";     // Default GET

    /// <summary>
    /// Exposed for testability — reads from the same config section.
    /// </summary>
    public static readonly RateLimitSettings DefaultSettings = new();

    public static IServiceCollection AddTeamFlowRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RateLimitSettings>(
            configuration.GetSection("RateLimiting"));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // ── Per-user per-endpoint sliding window policies ─────────────────

            options.AddPolicy(Auth, httpContext =>
            {
                var settings = GetSettings(httpContext);
                var partitionKey = BuildPartitionKey(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.AuthPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.AuthWindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = settings.QueueLimit,
                    });
            });

            options.AddPolicy(Write, httpContext =>
            {
                var settings = GetSettings(httpContext);
                var partitionKey = BuildPartitionKey(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.WritePermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WriteWindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = settings.QueueLimit,
                    });
            });

            options.AddPolicy(Search, httpContext =>
            {
                var settings = GetSettings(httpContext);
                var partitionKey = BuildPartitionKey(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.SearchPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.SearchWindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = settings.QueueLimit,
                    });
            });

            options.AddPolicy(BulkAction, httpContext =>
            {
                var settings = GetSettings(httpContext);
                var partitionKey = BuildPartitionKey(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.BulkPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.BulkWindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = settings.QueueLimit,
                    });
            });

            options.AddPolicy(General, httpContext =>
            {
                var settings = GetSettings(httpContext);
                var partitionKey = BuildPartitionKey(httpContext);

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GeneralPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.GeneralWindowSeconds),
                        SegmentsPerWindow = settings.SegmentsPerWindow,
                        QueueLimit = settings.QueueLimit,
                    });
            });

            // ── Rejection handler with structured logging ─────────────────────

            options.OnRejected = async (context, cancellationToken) =>
            {
                var loggerFactory = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("TeamFlow.RateLimiting");

                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userName = context.HttpContext.User?.Identity?.Name ?? "anonymous";
                var endpoint = context.HttpContext.Request.Path;

                logger.LogWarning(
                    "Rate limit exceeded for user {UserName} from IP {IpAddress} on endpoint {Endpoint}",
                    userName, ipAddress, endpoint);

                var settings = GetSettings(context.HttpContext);
                context.HttpContext.Response.Headers.RetryAfter =
                    settings.AuthWindowSeconds.ToString();

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit exceeded. Try again later."
                }, cancellationToken);
            };
        });

        return services;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RateLimitSettings GetSettings(HttpContext httpContext)
        => httpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<RateLimitSettings>>()
            .CurrentValue;

    private static string BuildPartitionKey(HttpContext httpContext)
    {
        var userName = httpContext.User?.Identity?.Name;
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userKey = userName ?? ipAddress ?? "anonymous";

        var endpoint = httpContext.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint;
        var routePattern = endpoint?.RoutePattern?.RawText ?? httpContext.Request.Path.ToString();

        return $"{userKey}:{routePattern}";
    }
}
