using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Services;
using TeamFlow.Tests.Common;

namespace TeamFlow.Infrastructure.Tests.Services;

public sealed class AdminSeedServiceTests : IntegrationTestBase
{
    private static IConfiguration BuildConfig(string email, string password) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemAdmin:Email"] = email,
                ["SystemAdmin:Password"] = password,
            })
            .Build();

    private AdminSeedService CreateService(IConfiguration config) =>
        new(config, Services.GetRequiredService<IServiceScopeFactory>(), Substitute.For<ILogger<AdminSeedService>>());

    [Fact]
    public async Task SeedAsync_WhenAdminNotExists_CreatesAdminUser()
    {
        var config = BuildConfig("admin@teamflow.dev", "Admin@1234");
        var service = CreateService(config);

        await service.StartAsync(CancellationToken.None);

        var admin = DbContext.Users.FirstOrDefault(u => u.Email == "admin@teamflow.dev");
        admin.Should().NotBeNull();
        admin!.SystemRole.Should().Be(SystemRole.SystemAdmin);
    }

    [Fact]
    public async Task SeedAsync_WhenUserExistsButNotAdmin_DoesNotPromote()
    {
        var email = "existing@teamflow.dev";
        var user = new Domain.Entities.User
        {
            Email = email,
            Name = "Existing",
            PasswordHash = "hashed",
            SystemRole = SystemRole.User
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var config = BuildConfig(email, "Admin@1234");
        var service = CreateService(config);

        await service.StartAsync(CancellationToken.None);
        DbContext.ChangeTracker.Clear();

        var notPromoted = DbContext.Users.FirstOrDefault(u => u.Email == email);
        notPromoted.Should().NotBeNull();
        notPromoted!.SystemRole.Should().Be(SystemRole.User);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminAlreadySystemAdmin_IsIdempotent()
    {
        var email = "idempotent@teamflow.dev";
        var user = new Domain.Entities.User
        {
            Email = email,
            Name = "Admin",
            PasswordHash = "hashed",
            SystemRole = SystemRole.SystemAdmin
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var config = BuildConfig(email, "Admin@1234");
        var service = CreateService(config);

        await service.StartAsync(CancellationToken.None);

        var count = DbContext.Users.Count(u => u.Email == email);
        count.Should().Be(1);
        DbContext.Users.First(u => u.Email == email).SystemRole.Should().Be(SystemRole.SystemAdmin);
    }

    [Fact]
    public async Task SeedAsync_WhenConfigMissing_DoesNotThrow()
    {
        var config = new ConfigurationBuilder().Build(); // no SystemAdmin section
        var service = CreateService(config);

        var act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
