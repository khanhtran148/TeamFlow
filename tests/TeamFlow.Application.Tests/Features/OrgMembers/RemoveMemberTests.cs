using FluentAssertions;
using TeamFlow.Application.Features.OrgMembers.Remove;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.OrgMembers;

[Collection("Projects")]
public sealed class RemoveMemberTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OwnerOrAdmin_CanRemoveMember(OrgRole currentUserRole)
    {
        var targetUser = UserBuilder.New().Build();
        var extraOwnerUser = UserBuilder.New().Build();
        DbContext.Users.AddRange(targetUser, extraOwnerUser);
        await DbContext.SaveChangesAsync();

        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(currentUserRole)
            .Build();
        var targetMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(targetUser.Id)
            .WithRole(OrgRole.Member)
            .Build();
        // Ensure there are at least 2 owners so removal doesn't hit last-owner guard
        var extraOwner = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(extraOwnerUser.Id)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.AddRange(actorMember, targetMember, extraOwner);
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveOrgMemberCommand(SeedOrgId, targetUser.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.OrganizationMembers.FindAsync(targetMember.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Member_ReturnsForbidden()
    {
        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Member)
            .Build();
        DbContext.OrganizationMembers.Add(actorMember);
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveOrgMemberCommand(SeedOrgId, Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var cmd = new RemoveOrgMemberCommand(SeedOrgId, Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_RemovingLastOwner_ReturnsValidationError()
    {
        var targetUser = UserBuilder.New().Build();
        DbContext.Users.Add(targetUser);

        // Actor is Admin (not owner), target is the only Owner
        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Admin)
            .Build();
        var targetMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(targetUser.Id)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.AddRange(actorMember, targetMember);
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveOrgMemberCommand(SeedOrgId, targetUser.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("last Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_RemovingSelf_ReturnsValidationError()
    {
        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.Add(actorMember);
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveOrgMemberCommand(SeedOrgId, SeedUserId);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("yourself", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsNotFound()
    {
        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.Add(actorMember);
        await DbContext.SaveChangesAsync();

        var nonMemberUserId = Guid.NewGuid();
        var cmd = new RemoveOrgMemberCommand(SeedOrgId, nonMemberUserId);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", o => o.IgnoringCase());
    }
}
