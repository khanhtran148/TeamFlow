using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.ListMyOrganizations;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Organizations;

public sealed class ListMyOrganizationsTests
{
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();

    public ListMyOrganizationsTests()
    {
        _currentUser.Id.Returns(_userId);
    }

    private ListMyOrganizationsHandler CreateHandler() => new(_memberRepo, _currentUser);

    [Fact]
    public async Task Handle_UserIsMemberOfOrgs_ReturnsMemberOrgDtos()
    {
        var org1 = new Organization { Name = "Org A", Slug = "org-a" };
        var org2 = new Organization { Name = "Org B", Slug = "org-b" };

        var memberships = new List<(Organization Org, OrgRole Role, DateTime JoinedAt)>
        {
            (org1, OrgRole.Owner, DateTime.UtcNow.AddDays(-10)),
            (org2, OrgRole.Member, DateTime.UtcNow.AddDays(-5)),
        };

        _memberRepo.ListOrganizationsForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(memberships);

        var result = await CreateHandler().Handle(new ListMyOrganizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var items = result.Value.ToList();
        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Org A");
        items[0].Role.Should().Be(OrgRole.Owner);
        items[1].Name.Should().Be("Org B");
        items[1].Role.Should().Be(OrgRole.Member);
    }

    [Fact]
    public async Task Handle_UserHasNoMemberships_ReturnsEmptyList()
    {
        _memberRepo.ListOrganizationsForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new List<(Organization, OrgRole, DateTime)>());

        var result = await CreateHandler().Handle(new ListMyOrganizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
