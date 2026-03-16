using FluentAssertions;
using TeamFlow.Application.Features.OrgMembers.ChangeRole;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.OrgMembers;

[Collection("Projects")]
public sealed class ChangeMemberRoleTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OwnerOrAdmin_CanChangeMemberRole(OrgRole currentUserRole)
    {
        var targetUser = UserBuilder.New().Build();
        var anotherOwnerUser = UserBuilder.New().Build();
        DbContext.Users.AddRange(targetUser, anotherOwnerUser);
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
        // Add a second owner so the actor can't be demoted as last owner concern
        var anotherOwner = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(anotherOwnerUser.Id)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.AddRange(actorMember, targetMember, anotherOwner);
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, targetUser.Id, OrgRole.Admin);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.OrganizationMembers.FindAsync(targetMember.Id);
        updated!.Role.Should().Be(OrgRole.Admin);
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

        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, Guid.NewGuid(), OrgRole.Admin);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        // SeedUserId has no membership in this org
        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, Guid.NewGuid(), OrgRole.Admin);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner or Admin", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_AdminPromotingToOwner_ReturnsForbidden()
    {
        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Admin)
            .Build();
        DbContext.OrganizationMembers.Add(actorMember);
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, Guid.NewGuid(), OrgRole.Owner);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_DemotingLastOwner_ReturnsValidationError()
    {
        var targetUser = UserBuilder.New().Build();
        DbContext.Users.Add(targetUser);

        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Owner)
            .Build();
        var targetMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(targetUser.Id)
            .WithRole(OrgRole.Owner)
            .Build();
        // Only 1 actual owner — actorMember is seeded user (Owner), targetMember also Owner
        // but we're demoting targetMember, making actor the last owner
        // Actually we need exactly 1 owner in total for the "last owner" guard to trigger
        // Remove actorMember from owner count by not seeding it — but actor must be owner to do the operation
        // Use a separate target-only-owner scenario: actor is Owner, target is the ONLY Owner
        DbContext.OrganizationMembers.AddRange(actorMember, targetMember);
        await DbContext.SaveChangesAsync();

        // actorMember is Owner and targetMember is Owner — 2 owners, but we want 1 for last-owner guard
        // Reconfigure: skip actorMember from owner role
        // Simplest approach: actor = Owner, target = Owner, total owners = 2 — NOT last owner
        // For last-owner test: actor = Owner, target = the ONLY owner (different user), total = 1
        // Let's redo with a fresh transaction-scoped scenario via DB manipulation
        // Since actorMember is Owner and targetMember is Owner, 2 owners total, so CountByRole returns 2
        // To simulate last-owner: we need only targetMember as Owner (actor = Admin)
        // But actor needs Owner or Admin to proceed
        // Correct scenario: actor = Owner, target = Owner, count = 1 (only target is owner)
        // But we already added actorMember as Owner -> count = 2
        // So let's use actor as Admin, target as last Owner
        // Remove actorMember and readd as Admin
        DbContext.ChangeTracker.Clear();
        var actor = await DbContext.OrganizationMembers.FindAsync(actorMember.Id);
        actor!.Role = OrgRole.Admin;
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, targetUser.Id, OrgRole.Admin);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("last Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_ChangingOwnRole_ReturnsValidationError()
    {
        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.Add(actorMember);
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, SeedUserId, OrgRole.Admin);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("own role", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_TargetNotFound_ReturnsNotFound()
    {
        var targetUser = UserBuilder.New().Build();
        DbContext.Users.Add(targetUser);

        var actorMember = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.Add(actorMember);
        await DbContext.SaveChangesAsync();

        // targetUser exists as User but NOT as OrganizationMember
        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, targetUser.Id, OrgRole.Admin);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Validate_InvalidRole_ReturnsValidationError()
    {
        var validator = new ChangeOrgMemberRoleValidator();
        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, Guid.NewGuid(), (OrgRole)99);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(ChangeOrgMemberRoleCommand.NewRole));
    }

    [Theory]
    [InlineData(OrgRole.Member)]
    [InlineData(OrgRole.Admin)]
    [InlineData(OrgRole.Owner)]
    public async Task Validate_ValidRole_IsValid(OrgRole role)
    {
        var validator = new ChangeOrgMemberRoleValidator();
        var cmd = new ChangeOrgMemberRoleCommand(SeedOrgId, Guid.NewGuid(), role);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }
}
