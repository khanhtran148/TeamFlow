using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Invitations.Accept;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Invitations;

[Collection("Auth")]
public sealed class AcceptInvitationTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    private async Task<(Organization org, Invitation invitation)> SeedPendingInvitationAsync(
        string rawToken = "validToken123",
        InviteStatus status = InviteStatus.Pending,
        DateTime? expiresAt = null)
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var invitation = InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash(ComputeHash(rawToken))
            .WithStatus(status)
            .WithExpiresAt(expiresAt ?? DateTime.UtcNow.AddDays(7))
            .Build();
        // Lazy-load navigation needed for slug
        DbContext.Set<Invitation>().Add(invitation);
        await DbContext.SaveChangesAsync();

        return (org, invitation);
    }

    [Fact]
    public async Task Handle_ValidToken_CreatesMembership()
    {
        const string rawToken = "validToken123";
        var (org, _) = await SeedPendingInvitationAsync(rawToken);

        var result = await Sender.Send(new AcceptInvitationCommand(rawToken));

        result.IsSuccess.Should().BeTrue();
        var member = await DbContext.Set<OrganizationMember>()
            .Where(m => m.UserId == SeedUserId && m.OrganizationId == org.Id)
            .FirstOrDefaultAsync();
        member.Should().NotBeNull();
        member!.Role.Should().Be(OrgRole.Member);
    }

    [Fact]
    public async Task Handle_ValidToken_UpdatesInvitationStatusToAccepted()
    {
        const string rawToken = "validToken456";
        var (_, invitation) = await SeedPendingInvitationAsync(rawToken);

        await Sender.Send(new AcceptInvitationCommand(rawToken));

        var updatedInv = await DbContext.Set<Invitation>().FindAsync(invitation.Id);
        updatedInv!.Status.Should().Be(InviteStatus.Accepted);
        updatedInv.AcceptedByUserId.Should().Be(SeedUserId);
        updatedInv.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsOrgInfo()
    {
        const string rawToken = "validToken789";
        var (org, _) = await SeedPendingInvitationAsync(rawToken);

        var result = await Sender.Send(new AcceptInvitationCommand(rawToken));

        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizationId.Should().Be(org.Id);
        result.Value.Role.Should().Be(OrgRole.Member);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        var result = await Sender.Send(new AcceptInvitationCommand("badtoken"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsBadRequest()
    {
        const string rawToken = "expiredToken";
        await SeedPendingInvitationAsync(rawToken, expiresAt: DateTime.UtcNow.AddMinutes(-1));

        var result = await Sender.Send(new AcceptInvitationCommand(rawToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("expired", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AlreadyAcceptedToken_ReturnsBadRequest()
    {
        const string rawToken = "acceptedToken";
        await SeedPendingInvitationAsync(rawToken, status: InviteStatus.Accepted);

        var result = await Sender.Send(new AcceptInvitationCommand(rawToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsBadRequest()
    {
        const string rawToken = "revokedToken";
        await SeedPendingInvitationAsync(rawToken, status: InviteStatus.Revoked);

        var result = await Sender.Send(new AcceptInvitationCommand(rawToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("revoked", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AlreadyMember_ReturnsBadRequest()
    {
        const string rawToken = "alreadyMemberToken";
        var (org, _) = await SeedPendingInvitationAsync(rawToken);

        // Make user already a member
        DbContext.Set<OrganizationMember>().Add(new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = SeedUserId,
            Role = OrgRole.Member
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AcceptInvitationCommand(rawToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already", Exactly.Once(), o => o.IgnoringCase());
    }
}
