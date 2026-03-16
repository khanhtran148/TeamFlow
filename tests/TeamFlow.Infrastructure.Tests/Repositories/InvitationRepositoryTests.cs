using FluentAssertions;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Infrastructure.Tests.Repositories;

public sealed class InvitationRepositoryTests : IntegrationTestBase
{
    private InvitationRepository CreateRepo() => new(DbContext);

    private const string PendingHash = "aaaa1111bbbb2222cccc3333dddd4444eeee5555ffff6666aaaa1111bbbb2222";

    [Fact]
    public async Task AddAsync_PersistsInvitation()
    {
        var invitation = InvitationBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash(PendingHash)
            .Build();

        await CreateRepo().AddAsync(invitation, CancellationToken.None);

        var saved = DbContext.Invitations.FirstOrDefault(i => i.TokenHash == PendingHash);
        saved.Should().NotBeNull();
        saved!.OrganizationId.Should().Be(SeedOrgId);
        saved.Status.Should().Be(InviteStatus.Pending);
    }

    [Fact]
    public async Task GetByTokenHashAsync_ExistingHash_ReturnsInvitation()
    {
        var hash = "bbbb2222cccc3333dddd4444eeee5555ffff6666aaaa1111bbbb2222cccc3333";
        var invitation = InvitationBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash(hash)
            .Build();
        DbContext.Invitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        var result = await CreateRepo().GetByTokenHashAsync(hash, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TokenHash.Should().Be(hash);
        result.OrganizationId.Should().Be(SeedOrgId);
        result.Organization.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTokenHashAsync_UnknownHash_ReturnsNull()
    {
        var result = await CreateRepo().GetByTokenHashAsync("nonexistent-hash", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsInvitation()
    {
        var invitation = InvitationBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash("getbyid-hash-test-value-1234567890abcdef1234567890abcdef")
            .Build();
        DbContext.Invitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        var result = await CreateRepo().GetByIdAsync(invitation.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(invitation.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await CreateRepo().GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListByOrgAsync_ReturnsInvitationsForOrg()
    {
        var invitation1 = InvitationBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash("listorg-hash-aaa-1111-2222-3333-4444-5555-6666-7777-8888")
            .WithEmail("user1@example.com")
            .Build();
        var invitation2 = InvitationBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash("listorg-hash-bbb-1111-2222-3333-4444-5555-6666-7777-9999")
            .WithStatus(InviteStatus.Accepted)
            .Build();
        DbContext.Invitations.AddRange(invitation1, invitation2);
        await DbContext.SaveChangesAsync();

        var results = await CreateRepo().ListByOrgAsync(SeedOrgId, CancellationToken.None);

        results.Should().HaveCountGreaterThanOrEqualTo(2);
        results.Should().Contain(i => i.TokenHash == "listorg-hash-aaa-1111-2222-3333-4444-5555-6666-7777-8888");
        results.Should().Contain(i => i.TokenHash == "listorg-hash-bbb-1111-2222-3333-4444-5555-6666-7777-9999");
    }

    [Fact]
    public async Task UpdateAsync_ChangesStatus()
    {
        var invitation = InvitationBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithInvitedBy(SeedUserId)
            .WithTokenHash("update-status-hash-aaa-bbb-ccc-ddd-eee-fff-111-222-333-444")
            .WithStatus(InviteStatus.Pending)
            .Build();
        DbContext.Invitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        invitation.Status = InviteStatus.Revoked;
        await CreateRepo().UpdateAsync(invitation, CancellationToken.None);

        var updated = DbContext.Invitations.Find(invitation.Id);
        updated!.Status.Should().Be(InviteStatus.Revoked);
    }
}
