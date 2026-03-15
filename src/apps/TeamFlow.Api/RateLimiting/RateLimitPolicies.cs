using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TeamFlow.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string Auth       = "auth";        // Login, Register
    public const string Write      = "write";       // POST, PUT, DELETE
    public const string Search     = "search";      // Search endpoints
    public const string BulkAction = "bulk_action"; // Bulk operations
    public const string General    = "general";     // Default GET

    public static IServiceCollection AddTeamFlowRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Auth: strict — 5 req/min
            options.AddFixedWindowLimiter(Auth, cfg =>
            {
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.PermitLimit = 5;
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            // Write: 30 req/min
            options.AddFixedWindowLimiter(Write, cfg =>
            {
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.PermitLimit = 30;
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 5;
            });

            // Search: 20 req/min (can be expensive)
            options.AddFixedWindowLimiter(Search, cfg =>
            {
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.PermitLimit = 20;
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 5;
            });

            // Bulk: 5 req/min
            options.AddFixedWindowLimiter(BulkAction, cfg =>
            {
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.PermitLimit = 5;
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 0;
            });

            // General: 100 req/min
            options.AddFixedWindowLimiter(General, cfg =>
            {
                cfg.Window = TimeSpan.FromMinutes(1);
                cfg.PermitLimit = 100;
                cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                cfg.QueueLimit = 10;
            });
        });

        return services;
    }
}
