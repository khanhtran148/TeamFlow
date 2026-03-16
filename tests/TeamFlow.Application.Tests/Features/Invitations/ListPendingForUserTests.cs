using FluentAssertions;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.ListPendingForUser;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Invitations;

[Collection("Auth")]
public sealed class ListPendingForUserTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    // TestCurrentUser has Email = "test@teamflow.dev"
    private const string UserEmail = "test@teamflow.dev";

    private async Task<Organization> SeedOrgAsync()
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();
        return org;
    }

    [Fact]
    public async Task Handle_ReturnsPendingInvitationsMatchingUserEmail()
    {
        var org = await SeedOrgAsync();
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithEmail(UserEmail)
            .WithStatus(InviteStatus.Pending)
            .WithExpiresAt(DateTime.UtcNow.AddDays(5))
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListPendingForUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ExcludesExpiredInvitations()
    {
        var org = await SeedOrgAsync();
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithEmail(UserEmail)
            .WithStatus(InviteStatus.Pending)
            .WithExpiresAt(DateTime.UtcNow.AddDays(-1))
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListPendingForUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesRevokedInvitations()
    {
        var org = await SeedOrgAsync();
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithEmail(UserEmail)
            .WithStatus(InviteStatus.Revoked)
            .WithExpiresAt(DateTime.UtcNow.AddDays(5))
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListPendingForUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExcludesAcceptedInvitations()
    {
        var org = await SeedOrgAsync();
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithEmail(UserEmail)
            .WithStatus(InviteStatus.Accepted)
            .WithExpiresAt(DateTime.UtcNow.AddDays(5))
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListPendingForUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsEmptyWhenNoInvitations()
    {
        var result = await Sender.Send(new ListPendingForUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsMappedDto()
    {
        var org = await SeedOrgAsync();
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithEmail(UserEmail)
            .WithRole(OrgRole.Admin)
            .WithStatus(InviteStatus.Pending)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListPendingForUserQuery());

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();
        dto.OrganizationId.Should().Be(org.Id);
        dto.Role.Should().Be(OrgRole.Admin);
        dto.Status.Should().Be(InviteStatus.Pending);
    }
}
