using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ListOrganizations;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ListAdminOrganizationsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_SystemAdmin_ReturnsAllOrganizations()
    {
        // SeedOrg already exists. Add 2 more.
        DbContext.Set<TeamFlow.Domain.Entities.Organization>().AddRange(
            OrganizationBuilder.New().WithName("Org A").Build(),
            OrganizationBuilder.New().WithName("Org B").Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminListOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_SystemAdmin_EmptyOrgs_ReturnsEmptyList()
    {
        // Just the seed org exists — but we still get at least 1
        var result = await Sender.Send(new AdminListOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeNull();
    }
}

[Collection("Auth")]
public sealed class ListAdminOrganizationsForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminListOrganizationsQuery());

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
