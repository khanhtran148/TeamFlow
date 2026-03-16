using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ListUsers;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ListAdminUsersTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_SystemAdmin_ReturnsAllUsers()
    {
        DbContext.Set<TeamFlow.Domain.Entities.User>().AddRange(
            UserBuilder.New().WithEmail("lau-a@example.com").Build(),
            UserBuilder.New().WithEmail("lau-b@example.com").Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminListUsersQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_SystemAdmin_MapsSystemRoleToDto()
    {
        var adminUser = UserBuilder.New()
            .WithEmail("lau-admin@example.com")
            .WithSystemRole(SystemRole.SystemAdmin)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.User>().Add(adminUser);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminListUsersQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(u => u.SystemRole == SystemRole.SystemAdmin);
    }
}

[Collection("Auth")]
public sealed class ListAdminUsersForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminListUsersQuery());

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
