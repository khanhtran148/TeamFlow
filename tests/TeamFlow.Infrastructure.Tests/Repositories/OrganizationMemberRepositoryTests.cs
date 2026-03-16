using FluentAssertions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Infrastructure.Tests.Repositories;

public sealed class OrganizationMemberRepositoryTests : IntegrationTestBase
{
    private OrganizationMemberRepository CreateRepo() => new(DbContext);

    private async Task<Guid> CreateUserAsync(string email)
    {
        var user = UserBuilder.New().WithEmail(email).Build();
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task AddAsync_CreatesOrganizationMember()
    {
        var userId = await CreateUserAsync("member1@test.com");
        var member = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(userId)
            .WithRole(OrgRole.Member)
            .Build();

        await CreateRepo().AddAsync(member, CancellationToken.None);

        var saved = DbContext.OrganizationMembers
            .FirstOrDefault(m => m.OrganizationId == SeedOrgId && m.UserId == userId);
        saved.Should().NotBeNull();
        saved!.Role.Should().Be(OrgRole.Member);
    }

    [Fact]
    public async Task GetMemberRoleAsync_ExistingMember_ReturnsRole()
    {
        var userId = await CreateUserAsync("owner@test.com");
        var member = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(userId)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.Add(member);
        await DbContext.SaveChangesAsync();

        var role = await CreateRepo().GetMemberRoleAsync(SeedOrgId, userId, CancellationToken.None);

        role.Should().Be(OrgRole.Owner);
    }

    [Fact]
    public async Task GetMemberRoleAsync_NonMember_ReturnsNull()
    {
        var userId = await CreateUserAsync("nonmember@test.com");

        var role = await CreateRepo().GetMemberRoleAsync(SeedOrgId, userId, CancellationToken.None);

        role.Should().BeNull();
    }

    [Fact]
    public async Task IsMemberAsync_ExistingMember_ReturnsTrue()
    {
        var userId = await CreateUserAsync("checkmember@test.com");
        var member = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(userId)
            .WithRole(OrgRole.Admin)
            .Build();
        DbContext.OrganizationMembers.Add(member);
        await DbContext.SaveChangesAsync();

        var isMember = await CreateRepo().IsMemberAsync(SeedOrgId, userId, CancellationToken.None);

        isMember.Should().BeTrue();
    }

    [Fact]
    public async Task IsMemberAsync_NonMember_ReturnsFalse()
    {
        var userId = await CreateUserAsync("notamember@test.com");

        var isMember = await CreateRepo().IsMemberAsync(SeedOrgId, userId, CancellationToken.None);

        isMember.Should().BeFalse();
    }

    [Fact]
    public async Task ListOrganizationsForUserAsync_ReturnsMemberships()
    {
        var userId = await CreateUserAsync("listmember@test.com");

        // Create second org
        var org2 = OrganizationBuilder.New().WithName("Second Org").WithSlug("second-org").Build();
        DbContext.Organizations.Add(org2);
        await DbContext.SaveChangesAsync();

        DbContext.OrganizationMembers.Add(OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId).WithUser(userId).WithRole(OrgRole.Owner).Build());
        DbContext.OrganizationMembers.Add(OrganizationMemberBuilder.New()
            .WithOrganization(org2.Id).WithUser(userId).WithRole(OrgRole.Member).Build());
        await DbContext.SaveChangesAsync();

        var memberships = await CreateRepo().ListOrganizationsForUserAsync(userId, CancellationToken.None);

        memberships.Should().HaveCount(2);
        memberships.Should().Contain(m => m.Org.Id == SeedOrgId && m.Role == OrgRole.Owner);
        memberships.Should().Contain(m => m.Org.Id == org2.Id && m.Role == OrgRole.Member);
    }
}
