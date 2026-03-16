using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.ListPendingForUser;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Invitations;

public sealed class ListPendingForUserTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private const string UserEmail = "user@example.com";

    public ListPendingForUserTests()
    {
        _currentUser.Id.Returns(_userId);
        _currentUser.Email.Returns(UserEmail);
    }

    private ListPendingForUserHandler CreateHandler() =>
        new(_invitationRepo, _currentUser);

    [Fact]
    public async Task Handle_ReturnsPendingInvitationsMatchingUserEmail()
    {
        var pendingInvitation = new Invitation
        {
            OrganizationId = Guid.NewGuid(),
            Email = UserEmail,
            Role = OrgRole.Member,
            TokenHash = "hash1",
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            InvitedByUserId = Guid.NewGuid()
        };

        _invitationRepo.ListPendingByEmailAsync(UserEmail, Arg.Any<CancellationToken>())
            .Returns([pendingInvitation]);

        var result = await CreateHandler().Handle(
            new ListPendingForUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ExcludesExpiredInvitations()
    {
        var expiredInvitation = new Invitation
        {
            OrganizationId = Guid.NewGuid(),
            Email = UserEmail,
            Role = OrgRole.Member,
            TokenHash = "hash2",
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // expired
            InvitedByUserId = Guid.NewGuid()
        };

        _invitationRepo.ListPendingByEmailAsync(UserEmail, Arg.Any<CancellationToken>())
            .Returns([expiredInvitation]);

        var result = await CreateHandler().Handle(
            new ListPendingForUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesRevokedInvitations()
    {
        var revokedInvitation = new Invitation
        {
            OrganizationId = Guid.NewGuid(),
            Email = UserEmail,
            Role = OrgRole.Member,
            TokenHash = "hash-revoked",
            Status = InviteStatus.Revoked,
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            InvitedByUserId = Guid.NewGuid()
        };

        _invitationRepo.ListPendingByEmailAsync(UserEmail, Arg.Any<CancellationToken>())
            .Returns([revokedInvitation]);

        var result = await CreateHandler().Handle(
            new ListPendingForUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesAcceptedInvitations()
    {
        var acceptedInvitation = new Invitation
        {
            OrganizationId = Guid.NewGuid(),
            Email = UserEmail,
            Role = OrgRole.Member,
            TokenHash = "hash-accepted",
            Status = InviteStatus.Accepted,
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            InvitedByUserId = Guid.NewGuid(),
            AcceptedAt = DateTime.UtcNow.AddDays(-1),
            AcceptedByUserId = Guid.NewGuid()
        };

        _invitationRepo.ListPendingByEmailAsync(UserEmail, Arg.Any<CancellationToken>())
            .Returns([acceptedInvitation]);

        var result = await CreateHandler().Handle(
            new ListPendingForUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsEmptyWhenNoInvitations()
    {
        _invitationRepo.ListPendingByEmailAsync(UserEmail, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateHandler().Handle(
            new ListPendingForUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsMappedDto()
    {
        var orgId = Guid.NewGuid();
        var invitation = new Invitation
        {
            OrganizationId = orgId,
            Email = UserEmail,
            Role = OrgRole.Admin,
            TokenHash = "hash3",
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedByUserId = Guid.NewGuid()
        };

        _invitationRepo.ListPendingByEmailAsync(UserEmail, Arg.Any<CancellationToken>())
            .Returns([invitation]);

        var result = await CreateHandler().Handle(
            new ListPendingForUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();
        dto.OrganizationId.Should().Be(orgId);
        dto.Role.Should().Be(OrgRole.Admin);
        dto.Status.Should().Be(InviteStatus.Pending);
    }
}
