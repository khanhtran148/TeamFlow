using FluentAssertions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Services;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Infrastructure.Tests.Services;

public sealed class PermissionCheckerTests : IntegrationTestBase
{
    private PermissionChecker Checker => new(DbContext);

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid OtherProjectId = Guid.NewGuid();
    private static readonly Guid OrgAdminUserId = Guid.NewGuid();
    private static readonly Guid DevUserId = Guid.NewGuid();
    private static readonly Guid ViewerUserId = Guid.NewGuid();
    private static readonly Guid TeamDevUserId = Guid.NewGuid();
    private static readonly Guid OverrideUserId = Guid.NewGuid();
    private static readonly Guid NoMemberUserId = Guid.NewGuid();
    private static readonly Guid TeamId = Guid.NewGuid();

    private bool _seeded;

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        await SeedTestData();
        _seeded = true;
    }

    private async Task SeedTestData()
    {
        // Create users with well-known IDs
        foreach (var (id, email, name) in new[]
        {
            (OrgAdminUserId, "admin@test.com", "Admin"),
            (DevUserId, "dev@test.com", "Developer"),
            (ViewerUserId, "viewer@test.com", "Viewer"),
            (TeamDevUserId, "teamdev@test.com", "Team Dev"),
            (OverrideUserId, "override@test.com", "Override User"),
            (NoMemberUserId, "nobody@test.com", "Nobody"),
        })
        {
            var user = UserBuilder.New().WithEmail(email).WithName(name).Build();
            DbContext.Entry(user).Property(nameof(User.Id)).CurrentValue = id;
            DbContext.Users.Add(user);
        }

        // Create project with well-known ID
        var project = ProjectBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithName("Test Project")
            .Build();
        DbContext.Entry(project).Property(nameof(Project.Id)).CurrentValue = ProjectId;
        DbContext.Projects.Add(project);

        // OrgAdmin has Org_Admin role on a project in the same org (legacy, kept for compatibility)
        DbContext.ProjectMemberships.Add(ProjectMembershipBuilder.New()
            .WithProject(ProjectId).WithMember(OrgAdminUserId).WithRole(ProjectRole.OrgAdmin).Build());

        // OrgAdmin is also an Owner in the OrganizationMember table (new system)
        DbContext.OrganizationMembers.Add(OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(OrgAdminUserId)
            .WithRole(OrgRole.Owner)
            .Build());

        // Dev user has individual Developer role
        DbContext.ProjectMemberships.Add(ProjectMembershipBuilder.New()
            .WithProject(ProjectId).WithMember(DevUserId).WithRole(ProjectRole.Developer).Build());

        // Viewer user has individual Viewer role
        DbContext.ProjectMemberships.Add(ProjectMembershipBuilder.New()
            .WithProject(ProjectId).WithMember(ViewerUserId).AsViewer().Build());

        // Team with TeamDevUser and OverrideUser as members
        var team = TeamBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithName("Dev Team")
            .Build();
        DbContext.Entry(team).Property(nameof(Team.Id)).CurrentValue = TeamId;
        DbContext.Teams.Add(team);

        DbContext.TeamMembers.Add(new TeamMember { TeamId = TeamId, UserId = TeamDevUserId, Role = ProjectRole.Developer });
        DbContext.TeamMembers.Add(new TeamMember { TeamId = TeamId, UserId = OverrideUserId, Role = ProjectRole.Developer });

        // Team has Developer role on project
        DbContext.ProjectMemberships.Add(ProjectMembershipBuilder.New()
            .WithProject(ProjectId).WithMember(TeamId).WithMemberType("Team").WithRole(ProjectRole.Developer).Build());

        // Override user: Developer via team, but TechnicalLeader via individual override
        DbContext.ProjectMemberships.Add(ProjectMembershipBuilder.New()
            .WithProject(ProjectId).WithMember(OverrideUserId).WithRole(ProjectRole.TechnicalLeader).Build());

        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task OrgAdmin_AlwaysHasPermission()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            OrgAdminUserId, ProjectId, Permission.WorkItem_Delete);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task OrgAdmin_HasPermissionOnAnyProject()
    {
        await EnsureSeededAsync();
        var project2 = ProjectBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithName("Other Project")
            .Build();
        DbContext.Entry(project2).Property(nameof(Project.Id)).CurrentValue = OtherProjectId;
        DbContext.Projects.Add(project2);
        await DbContext.SaveChangesAsync();

        var result = await Checker.HasPermissionAsync(
            OrgAdminUserId, OtherProjectId, Permission.Project_Edit);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Developer_CanCreateWorkItems()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            DevUserId, ProjectId, Permission.WorkItem_Create);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Developer_CannotDeleteProjects()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            DevUserId, ProjectId, Permission.Project_Edit);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Viewer_CannotCreateWorkItems()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            ViewerUserId, ProjectId, Permission.WorkItem_Create);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TeamMember_InheritsTeamProjectRole()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            TeamDevUserId, ProjectId, Permission.WorkItem_Create);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IndividualOverride_TakesPrecedenceOverTeamRole()
    {
        await EnsureSeededAsync();
        var effectiveRole = await Checker.GetEffectiveRoleAsync(
            OverrideUserId, ProjectId);

        effectiveRole.Should().Be(ProjectRole.TechnicalLeader);
    }

    [Fact]
    public async Task NoMembership_ReturnsFalse()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            NoMemberUserId, ProjectId, Permission.WorkItem_View);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task NoMembership_ReturnsNullRole()
    {
        await EnsureSeededAsync();
        var role = await Checker.GetEffectiveRoleAsync(
            NoMemberUserId, ProjectId);

        role.Should().BeNull();
    }

    [Fact]
    public async Task NonExistentProject_ReturnsFalse()
    {
        await EnsureSeededAsync();
        var result = await Checker.HasPermissionAsync(
            DevUserId, Guid.NewGuid(), Permission.WorkItem_View);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task OrgMember_WithOwnerRole_IsOrgAdmin()
    {
        await EnsureSeededAsync();

        // Add OrgMember entry for the OrgAdmin user with Owner role
        var ownerUserId = Guid.NewGuid();
        var user = UserBuilder.New().WithEmail("orgowner@test.com").Build();
        DbContext.Entry(user).Property(nameof(User.Id)).CurrentValue = ownerUserId;
        DbContext.Users.Add(user);

        DbContext.OrganizationMembers.Add(
            OrganizationMemberBuilder.New()
                .WithOrganization(SeedOrgId)
                .WithUser(ownerUserId)
                .WithRole(OrgRole.Owner)
                .Build());
        await DbContext.SaveChangesAsync();

        var result = await Checker.HasPermissionAsync(
            ownerUserId, SeedOrgId, Permission.Org_Admin);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task OrgMember_WithAdminRole_IsOrgAdmin()
    {
        await EnsureSeededAsync();

        var adminUserId = Guid.NewGuid();
        var user = UserBuilder.New().WithEmail("orgadmin2@test.com").Build();
        DbContext.Entry(user).Property(nameof(User.Id)).CurrentValue = adminUserId;
        DbContext.Users.Add(user);

        DbContext.OrganizationMembers.Add(
            OrganizationMemberBuilder.New()
                .WithOrganization(SeedOrgId)
                .WithUser(adminUserId)
                .WithRole(OrgRole.Admin)
                .Build());
        await DbContext.SaveChangesAsync();

        var result = await Checker.HasPermissionAsync(
            adminUserId, SeedOrgId, Permission.Org_Admin);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task OrgMember_WithMemberRole_IsNotOrgAdmin()
    {
        await EnsureSeededAsync();

        var memberUserId = Guid.NewGuid();
        var user = UserBuilder.New().WithEmail("orgmember@test.com").Build();
        DbContext.Entry(user).Property(nameof(User.Id)).CurrentValue = memberUserId;
        DbContext.Users.Add(user);

        DbContext.OrganizationMembers.Add(
            OrganizationMemberBuilder.New()
                .WithOrganization(SeedOrgId)
                .WithUser(memberUserId)
                .WithRole(OrgRole.Member)
                .Build());
        await DbContext.SaveChangesAsync();

        var result = await Checker.HasPermissionAsync(
            memberUserId, SeedOrgId, Permission.Org_Admin);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task NoOrganizationMember_IsNotOrgAdmin_EvenWithNoAdminsExisting()
    {
        // This tests that the bootstrap hack is gone —
        // having no org members should NOT grant access to everyone.
        await EnsureSeededAsync();

        // Create a fresh org with NO members
        var freshOrgId = Guid.NewGuid();
        var freshOrg = OrganizationBuilder.New().WithName("Fresh Org").WithSlug("fresh-org").Build();
        DbContext.Entry(freshOrg).Property(nameof(Organization.Id)).CurrentValue = freshOrgId;
        DbContext.Organizations.Add(freshOrg);
        await DbContext.SaveChangesAsync();

        var result = await Checker.HasPermissionAsync(
            NoMemberUserId, freshOrgId, Permission.Org_Admin);

        // Bootstrap hack removed: should be false, not true
        result.Should().BeFalse();
    }
}
