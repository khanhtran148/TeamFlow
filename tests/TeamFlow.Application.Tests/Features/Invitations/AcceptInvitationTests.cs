using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Invitations.Accept;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Invitations;

public sealed class AcceptInvitationTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();

    public AcceptInvitationTests()
    {
        _currentUser.Id.Returns(_userId);

        _memberRepo.IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _memberRepo.AddAsync(Arg.Any<OrganizationMember>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<OrganizationMember>());
        _invitationRepo.UpdateAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Invitation>());
    }

    private AcceptInvitationHandler CreateHandler() =>
        new(_invitationRepo, _memberRepo, _currentUser);

    private Invitation MakePendingInvitation() => new()
    {
        OrganizationId = _orgId,
        InvitedByUserId = Guid.NewGuid(),
        Role = OrgRole.Member,
        TokenHash = "somehash",
        Status = InviteStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        Organization = new Organization { Name = "Test Org", Slug = "test-org" }
    };

    [Fact]
    public async Task Handle_ValidToken_CreatesMembership()
    {
        const string rawToken = "validToken123";
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _memberRepo.Received(1).AddAsync(
            Arg.Is<OrganizationMember>(m =>
                m.UserId == _userId &&
                m.OrganizationId == _orgId &&
                m.Role == OrgRole.Member),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidToken_UpdatesInvitationStatusToAccepted()
    {
        const string rawToken = "validToken123";
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        await CreateHandler().Handle(new AcceptInvitationCommand(rawToken), CancellationToken.None);

        await _invitationRepo.Received(1).UpdateAsync(
            Arg.Is<Invitation>(i =>
                i.Status == InviteStatus.Accepted &&
                i.AcceptedByUserId == _userId &&
                i.AcceptedAt.HasValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsOrgInfo()
    {
        const string rawToken = "validToken123";
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizationId.Should().Be(_orgId);
        result.Value.OrganizationSlug.Should().Be("test-org");
        result.Value.Role.Should().Be(OrgRole.Member);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Invitation?)null);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand("badtoken"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsBadRequest()
    {
        var invitation = MakePendingInvitation();
        invitation.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand("expiredtoken"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("expired", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedToken_ReturnsBadRequest()
    {
        var invitation = MakePendingInvitation();
        invitation.Status = InviteStatus.Accepted;
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand("acceptedtoken"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsBadRequest()
    {
        var invitation = MakePendingInvitation();
        invitation.Status = InviteStatus.Revoked;
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand("revokedtoken"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("revoked", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AlreadyMember_ReturnsBadRequest()
    {
        var invitation = MakePendingInvitation();
        _invitationRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);
        _memberRepo.IsMemberAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(new AcceptInvitationCommand("validtoken"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already", Exactly.Once(), o => o.IgnoringCase());
    }
}
