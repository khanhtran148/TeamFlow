using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.List;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Invitations;

public sealed class ListInvitationsTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ListInvitationsTests()
    {
        _currentUser.Id.Returns(_userId);
    }

    private ListInvitationsHandler CreateHandler() =>
        new(_invitationRepo, _memberRepo, _currentUser);

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OrgOwnerOrAdmin_ReturnsInvitations(OrgRole role)
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(role);
        _invitationRepo.ListByOrgAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns([
                new Invitation
                {
                    OrganizationId = _orgId,
                    InvitedByUserId = _userId,
                    Role = OrgRole.Member,
                    TokenHash = "hash1",
                    Status = InviteStatus.Pending,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                }
            ]);

        var result = await CreateHandler().Handle(new ListInvitationsQuery(_orgId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(OrgRole.Member);

        var result = await CreateHandler().Handle(new ListInvitationsQuery(_orgId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns((OrgRole?)null);

        var result = await CreateHandler().Handle(new ListInvitationsQuery(_orgId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_OrgOwner_ReturnsMappedDtos()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>()).Returns(OrgRole.Owner);
        var invitation = new Invitation
        {
            OrganizationId = _orgId,
            InvitedByUserId = _userId,
            Email = "user@example.com",
            Role = OrgRole.Admin,
            TokenHash = "hashval",
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _invitationRepo.ListByOrgAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns([invitation]);

        var result = await CreateHandler().Handle(new ListInvitationsQuery(_orgId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();
        dto.Should().BeOfType<InvitationDto>();
        dto.Email.Should().Be("user@example.com");
        dto.Role.Should().Be(OrgRole.Admin);
        dto.Status.Should().Be(InviteStatus.Pending);
    }
}
