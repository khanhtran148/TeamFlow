using FluentAssertions;
using TeamFlow.Application.Features.Invitations.Revoke;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Invitations;

[Collection("Auth")]
public sealed class RevokeInvitationTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<(Organization org, Invitation invitation)> SeedPendingInvitationAsync(
        OrgRole memberRole = OrgRole.Owner,
        InviteStatus status = InviteStatus.Pending)
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        DbContext.Set<OrganizationMember>().Add(new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = SeedUserId,
            Role = memberRole
        });

        var invitation = InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithStatus(status)
            .Build();
        DbContext.Set<Invitation>().Add(invitation);
        await DbContext.SaveChangesAsync();

        return (org, invitation);
    }

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OrgOwnerOrAdmin_CanRevokeInvitation(OrgRole role)
    {
        var (_, invitation) = await SeedPendingInvitationAsync(role);

        var result = await Sender.Send(new RevokeInvitationCommand(invitation.Id));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrgOwner_SetsStatusToRevoked()
    {
        var (_, invitation) = await SeedPendingInvitationAsync(OrgRole.Owner);

        await Sender.Send(new RevokeInvitationCommand(invitation.Id));

        var updated = await DbContext.Set<Invitation>().FindAsync(invitation.Id);
        updated!.Status.Should().Be(InviteStatus.Revoked);
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        var (_, invitation) = await SeedPendingInvitationAsync(OrgRole.Member);

        var result = await Sender.Send(new RevokeInvitationCommand(invitation.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var invitation = InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithStatus(InviteStatus.Pending)
            .Build();
        DbContext.Set<Invitation>().Add(invitation);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RevokeInvitationCommand(invitation.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ReturnsNotFound()
    {
        var result = await Sender.Send(new RevokeInvitationCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedInvitation_ReturnsBadRequest()
    {
        var (_, invitation) = await SeedPendingInvitationAsync(OrgRole.Owner, InviteStatus.Accepted);

        var result = await Sender.Send(new RevokeInvitationCommand(invitation.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already", o => o.IgnoringCase());
        result.Error.Should().ContainEquivalentOf("accepted", o => o.IgnoringCase());
    }
}
