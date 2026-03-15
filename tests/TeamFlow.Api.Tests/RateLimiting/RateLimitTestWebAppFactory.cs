using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.RateLimiting;

/// <summary>
/// WebApplicationFactory with low rate limits for testing 429 responses.
/// Overrides WritePermitLimit=3, GeneralPermitLimit=3, Window=60s.
/// </summary>
internal sealed class RateLimitTestWebAppFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.ConnectionString);
        builder.UseSetting("Jwt:Secret", TestJwtSettings.Secret);
        builder.UseSetting("Jwt:Issuer", TestJwtSettings.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtSettings.Audience);

        // Override rate limit settings to low values for testability
        builder.UseSetting("RateLimiting:WritePermitLimit", "3");
        builder.UseSetting("RateLimiting:WriteWindowSeconds", "60");
        builder.UseSetting("RateLimiting:GeneralPermitLimit", "3");
        builder.UseSetting("RateLimiting:GeneralWindowSeconds", "60");
        builder.UseSetting("RateLimiting:SegmentsPerWindow", "1");

        builder.ConfigureServices(services =>
        {
            services.ReplaceService<IBroadcastService, NullBroadcastService>();
            services.ReplaceHealthCheck();
        });
    }

    public async Task EnsureDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
