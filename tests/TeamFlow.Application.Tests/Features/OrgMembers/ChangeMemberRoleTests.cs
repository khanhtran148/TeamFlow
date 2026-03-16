using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.OrgMembers.ChangeRole;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.OrgMembers;

public sealed class ChangeMemberRoleTests
{
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _targetUserId = Guid.NewGuid();

    public ChangeMemberRoleTests()
    {
        _currentUser.Id.Returns(_currentUserId);
    }

    private ChangeOrgMemberRoleHandler CreateHandler() => new(_memberRepo, _currentUser);

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OwnerOrAdmin_CanChangeMemberRole(OrgRole currentUserRole)
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

        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, OrgRole.Admin);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _memberRepo.Received(1).UpdateAsync(
            Arg.Is<OrganizationMember>(m => m.Role == OrgRole.Admin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Member_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Member);

        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, OrgRole.Admin);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns((OrgRole?)null);

        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, OrgRole.Admin);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AdminPromotingToOwner_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Admin);

        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, OrgRole.Owner);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_DemotingLastOwner_ReturnsValidationError()
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

        // Only 1 owner in the org
        _memberRepo.CountByRoleAsync(_orgId, OrgRole.Owner, Arg.Any<CancellationToken>())
            .Returns(1);

        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, OrgRole.Admin);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("last Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_ChangingOwnRole_ReturnsValidationError()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);

        // Target is the current user (self)
        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _currentUserId, OrgRole.Admin);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("own role", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsNotFound()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _currentUserId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);

        _memberRepo.GetByOrgAndUserAsync(_orgId, _targetUserId, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        _memberRepo.CountByRoleAsync(_orgId, OrgRole.Owner, Arg.Any<CancellationToken>())
            .Returns(1);

        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, OrgRole.Admin);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Validate_InvalidRole_ReturnsValidationError()
    {
        var validator = new ChangeOrgMemberRoleValidator();
        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, (OrgRole)99);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(ChangeOrgMemberRoleCommand.NewRole));
    }

    [Theory]
    [InlineData(OrgRole.Member)]
    [InlineData(OrgRole.Admin)]
    [InlineData(OrgRole.Owner)]
    public async Task Validate_ValidRole_IsValid(OrgRole role)
    {
        var validator = new ChangeOrgMemberRoleValidator();
        var cmd = new ChangeOrgMemberRoleCommand(_orgId, _targetUserId, role);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }
}
