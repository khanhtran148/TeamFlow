using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Admin.ListOrganizations;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ListAdminOrganizationsPagedTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminListOrganizationsHandler CreateHandler() => new(_orgRepo, _currentUser);

    private void SetupSystemAdmin() =>
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResult()
    {
        SetupSystemAdmin();
        var orgs = Enumerable.Range(1, 5)
            .Select(i => OrganizationBuilder.New().WithName($"Org {i}").Build())
            .ToList();
        _orgRepo.ListAllPagedAsync(null, 1, 3, Arg.Any<CancellationToken>())
            .Returns((orgs.Take(3).ToList() as IEnumerable<Organization>, 5));
        var query = new AdminListOrganizationsQuery(null, 1, 3);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SearchByName_FiltersResults()
    {
        SetupSystemAdmin();
        var teamflow = OrganizationBuilder.New().WithName("TeamFlow Inc").Build();
        _orgRepo.ListAllPagedAsync("teamflow", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Organization> { teamflow } as IEnumerable<Organization>, 1));
        var query = new AdminListOrganizationsQuery("teamflow", 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().Name.Should().Be("TeamFlow Inc");
    }

    [Fact]
    public async Task Handle_OrgDto_IncludesSlugMemberCountIsActive()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New()
            .WithName("TeamFlow Inc")
            .WithSlug("teamflow-inc")
            .WithIsActive(false)
            .Build();
        _orgRepo.ListAllPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Organization> { org } as IEnumerable<Organization>, 1));
        var query = new AdminListOrganizationsQuery(null, 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items.Single();
        dto.Slug.Should().Be("teamflow-inc");
        dto.IsActive.Should().BeFalse();
        dto.MemberCount.Should().Be(org.Members.Count);
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var query = new AdminListOrganizationsQuery(null, 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
