using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Invitations.Revoke;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Invitations;

public sealed class RevokeInvitationTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _invitationId = Guid.NewGuid();

    public RevokeInvitationTests()
    {
        _currentUser.Id.Returns(_userId);
        _invitationRepo.UpdateAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Invitation>());
    }

    private RevokeInvitationHandler CreateHandler() =>
        new(_invitationRepo, _memberRepo, _currentUser);

    private Invitation MakePendingInvitation() => new()
    {
        OrganizationId = _orgId,
        InvitedByUserId = Guid.NewGuid(),
        Role = OrgRole.Member,
        TokenHash = "somehash",
        Status = InviteStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OrgOwnerOrAdmin_CanRevokeInvitation(OrgRole role)
    {
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(invitation);
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(role);

        var result = await CreateHandler().Handle(new RevokeInvitationCommand(_invitationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrgOwner_SetsStatusToRevoked()
    {
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(invitation);
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(OrgRole.Owner);

        await CreateHandler().Handle(new RevokeInvitationCommand(_invitationId), CancellationToken.None);

        await _invitationRepo.Received(1).UpdateAsync(
            Arg.Is<Invitation>(i => i.Status == InviteStatus.Revoked),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(invitation);
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(OrgRole.Member);

        var result = await CreateHandler().Handle(new RevokeInvitationCommand(_invitationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(invitation);
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns((OrgRole?)null);

        var result = await CreateHandler().Handle(new RevokeInvitationCommand(_invitationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ReturnsNotFound()
    {
        _invitationRepo.GetByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns((Invitation?)null);

        var result = await CreateHandler().Handle(new RevokeInvitationCommand(_invitationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedInvitation_ReturnsBadRequest()
    {
        var invitation = MakePendingInvitation();
        invitation.Status = InviteStatus.Accepted;
        _invitationRepo.GetByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(invitation);
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(OrgRole.Owner);

        var result = await CreateHandler().Handle(new RevokeInvitationCommand(_invitationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already", o => o.IgnoringCase());
        result.Error.Should().ContainEquivalentOf("accepted", o => o.IgnoringCase());
    }
}
