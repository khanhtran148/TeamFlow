using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Services;

public sealed class AdminSeedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<AdminSeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SystemAdmin:Email"];
        var password = configuration["SystemAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SystemAdmin:Email or SystemAdmin:Password not configured — skipping admin seed.");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();

        var existing = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existing is null)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            var admin = new User
            {
                Email = email,
                Name = "System Admin",
                PasswordHash = passwordHash,
                SystemRole = SystemRole.SystemAdmin
            };
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("System admin user created: {Email}", email);
        }
        else if (existing.SystemRole != SystemRole.SystemAdmin)
        {
            logger.LogWarning(
                "User {Email} exists but is not a SystemAdmin. Manual promotion required — " +
                "the seed service will not auto-promote existing accounts for security.",
                email);
        }
        else
        {
            logger.LogInformation("System admin {Email} already exists — no action needed.", email);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
