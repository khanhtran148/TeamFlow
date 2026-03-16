using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.OrgMembers;
using TeamFlow.Application.Features.OrgMembers.List;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.OrgMembers;

public sealed class ListMembersTests
{
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ListMembersTests()
    {
        _currentUser.Id.Returns(_userId);
    }

    private ListOrgMembersHandler CreateHandler() => new(_memberRepo, _currentUser);

    [Fact]
    public async Task Handle_OrgMember_ReturnsMembers()
    {
        _memberRepo.IsMemberAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(true);

        var members = new List<(OrganizationMember Member, User User)>
        {
            (
                new OrganizationMember { OrganizationId = _orgId, UserId = _userId, Role = OrgRole.Owner },
                new User { Name = "Test User", Email = "test@example.com" }
            )
        };

        _memberRepo.ListByOrgWithUsersAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(members);

        var result = await CreateHandler().Handle(new ListOrgMembersQuery(_orgId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.IsMemberAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new ListOrgMembersQuery(_orgId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("member", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_OrgMember_MapsDtoCorrectly()
    {
        _memberRepo.IsMemberAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(true);

        var members = new List<(OrganizationMember Member, User User)>
        {
            (
                new OrganizationMember
                {
                    OrganizationId = _orgId,
                    UserId = _userId,
                    Role = OrgRole.Admin
                },
                new User { Name = "Alice Admin", Email = "alice@example.com" }
            )
        };

        _memberRepo.ListByOrgWithUsersAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(members);

        var result = await CreateHandler().Handle(new ListOrgMembersQuery(_orgId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.UserId.Should().Be(_userId);
        dto.UserName.Should().Be("Alice Admin");
        dto.UserEmail.Should().Be("alice@example.com");
        dto.Role.Should().Be(OrgRole.Admin);
    }
}
