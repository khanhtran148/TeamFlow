using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.OrgMembers.Remove;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.OrgMembers;

public sealed class RemoveMemberTests
{
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public RemoveMemberTests()
    {
        _currentUser.Id.Returns(_currentUserId);
    }

    private RemoveOrgMemberHandler CreateHandler() => new(_memberRepo, _currentUser);

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OwnerOrAdmin_CanRemoveMember(OrgRole currentUserRole)
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(currentUserRole);

        var targetMember = new OrganizationMember
        {
            OrganizationId = _orgId,
            UserId = _targetUserId,
            Role = OrgRole.Member
        };
        _memberRepo.GetByOrgAndUserAsync(_orgId, _targetUserId, Arg.Any<CancellationToken>())
            .Returns(targetMember);

        _memberRepo.CountByRoleAsync(_orgId, OrgRole.Owner, Arg.Any<CancellationToken>())
            .Returns(2);

        var cmd = new RemoveOrgMemberCommand(_orgId, _targetUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _memberRepo.Received(1).DeleteAsync(targetMember, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Member_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Member);

        var cmd = new RemoveOrgMemberCommand(_orgId, _targetUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns((OrgRole?)null);

        var cmd = new RemoveOrgMemberCommand(_orgId, _targetUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_RemovingLastOwner_ReturnsValidationError()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);

        var targetMember = new OrganizationMember
        {
            OrganizationId = _orgId,
            UserId = _targetUserId,
            Role = OrgRole.Owner
        };
        _memberRepo.GetByOrgAndUserAsync(_orgId, _targetUserId, Arg.Any<CancellationToken>())
            .Returns(targetMember);

        _memberRepo.CountByRoleAsync(_orgId, OrgRole.Owner, Arg.Any<CancellationToken>())
            .Returns(1);

        var cmd = new RemoveOrgMemberCommand(_orgId, _targetUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("last Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_RemovingSelf_ReturnsValidationError()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);

        // Target is the current user (self-removal)
        var cmd = new RemoveOrgMemberCommand(_orgId, _currentUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("yourself", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsNotFound()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);

        _memberRepo.GetByOrgAndUserAsync(_orgId, _targetUserId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        var cmd = new RemoveOrgMemberCommand(_orgId, _targetUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", o => o.IgnoringCase());
    }
}
