using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Tests.Common;

/// <summary>
/// Centralizes reference data seeding for integration tests.
/// Seeds the well-known Organization and User required by all test bases.
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedReferenceDataAsync(TeamFlowDbContext ctx, Guid orgId, Guid userId)
    {
        if (!await ctx.Set<Organization>().AnyAsync(o => o.Id == orgId))
        {
            var org = new Organization { Name = "Test Org" };
            ctx.Entry(org).Property(nameof(Organization.Id)).CurrentValue = orgId;
            ctx.Set<Organization>().Add(org);
        }

        if (!await ctx.Set<User>().AnyAsync(u => u.Id == userId))
        {
            var user = new User { Email = "test@teamflow.dev", Name = "Test User", PasswordHash = "not-a-real-hash" };
            ctx.Entry(user).Property(nameof(User.Id)).CurrentValue = userId;
            ctx.Set<User>().Add(user);
        }

        await ctx.SaveChangesAsync();
    }
}
