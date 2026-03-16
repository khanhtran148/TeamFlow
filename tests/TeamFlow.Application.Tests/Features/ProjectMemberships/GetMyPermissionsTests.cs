using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.GetMyPermissions;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

[Collection("Projects")]
public sealed class GetMyPermissionsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_UserWithRole_ReturnsRoleAndPermissions()
    {
        var project = await SeedProjectAsync();

        // AlwaysAllowTestPermissionChecker returns ProjectRole.Developer
        var result = await Sender.Send(new GetMyPermissionsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Developer");
        result.Value.Permissions.Should().NotBeEmpty();
    }
}

[Collection("Projects")]
public sealed class GetMyPermissionsNoRoleTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_UserWithNoRole_ReturnsEmptyPermissions()
    {
        var project = await SeedProjectAsync();

        // AlwaysDenyTestPermissionChecker returns null for GetEffectiveRoleAsync
        var result = await Sender.Send(new GetMyPermissionsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().BeNull();
        result.Value.Permissions.Should().BeEmpty();
    }
}
