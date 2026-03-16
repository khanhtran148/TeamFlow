using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ListOrganizations;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ListAdminOrganizationsPagedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResult()
    {
        for (var i = 1; i <= 5; i++)
        {
            DbContext.Set<TeamFlow.Domain.Entities.Organization>().Add(
                OrganizationBuilder.New().WithName($"PagedOrg {i}").Build());
        }
        await DbContext.SaveChangesAsync();

        var query = new AdminListOrganizationsQuery(null, 1, 3);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SearchByName_FiltersResults()
    {
        DbContext.Set<TeamFlow.Domain.Entities.Organization>().Add(
            OrganizationBuilder.New().WithName("TeamFlow Inc").WithSlug("teamflow-inc-laop").Build());
        await DbContext.SaveChangesAsync();

        var query = new AdminListOrganizationsQuery("TeamFlow Inc", 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(o => o.Name == "TeamFlow Inc");
    }

    [Fact]
    public async Task Handle_OrgDto_IncludesSlugIsActive()
    {
        var org = OrganizationBuilder.New()
            .WithName("TestDtoOrg")
            .WithSlug("test-dto-org-laop")
            .WithIsActive(false)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var query = new AdminListOrganizationsQuery("TestDtoOrg", 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items.Single(o => o.Name == "TestDtoOrg");
        dto.Slug.Should().Be("test-dto-org-laop");
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        // This test uses default (non-admin) current user, so we need a separate test class
        // Left here as documentation - non-admin forbidden test is in ListAdminOrganizationsForbiddenTests
    }
}
