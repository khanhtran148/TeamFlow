using FluentAssertions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Authorization;

public sealed class PermissionMatrixTests
{
    [Fact]
    public void OrgAdmin_HasAllPermissions()
    {
        var allPermissions = Enum.GetValues<Permission>();

        foreach (var perm in allPermissions)
        {
            PermissionMatrix.RoleHasPermission(ProjectRole.OrgAdmin, perm)
                .Should().BeTrue($"OrgAdmin should have {perm}");
        }
    }

    [Theory]
    [InlineData(Permission.Project_View)]
    [InlineData(Permission.WorkItem_View)]
    [InlineData(Permission.Sprint_View)]
    [InlineData(Permission.Release_View)]
    [InlineData(Permission.Retro_View)]
    [InlineData(Permission.Comment_View)]
    [InlineData(Permission.Poker_View)]
    [InlineData(Permission.Notification_View)]
    public void Viewer_HasOnlyViewPermissions(Permission permission)
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.Viewer, permission)
            .Should().BeTrue($"Viewer should have {permission}");
    }

    [Theory]
    [InlineData(Permission.WorkItem_Create)]
    [InlineData(Permission.WorkItem_Edit)]
    [InlineData(Permission.WorkItem_Delete)]
    [InlineData(Permission.Project_Edit)]
    [InlineData(Permission.Sprint_Start)]
    public void Viewer_CannotMutate(Permission permission)
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.Viewer, permission)
            .Should().BeFalse($"Viewer should NOT have {permission}");
    }

    [Fact]
    public void Developer_CannotDeleteProjects()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.Developer, Permission.Project_Edit)
            .Should().BeFalse();
        PermissionMatrix.RoleHasPermission(ProjectRole.Developer, Permission.Project_Archive)
            .Should().BeFalse();
    }

    [Fact]
    public void ProductOwner_CannotStartSprints()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.ProductOwner, Permission.Sprint_Start)
            .Should().BeFalse();
        PermissionMatrix.RoleHasPermission(ProjectRole.ProductOwner, Permission.Sprint_Complete)
            .Should().BeFalse();
    }

    [Fact]
    public void TechLead_CanChangeStatus()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.TechnicalLeader, Permission.WorkItem_ChangeStatus)
            .Should().BeTrue();
    }

    [Fact]
    public void TeamManager_CanStartSprints()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.TeamManager, Permission.Sprint_Start)
            .Should().BeTrue();
        PermissionMatrix.RoleHasPermission(ProjectRole.TeamManager, Permission.Sprint_Complete)
            .Should().BeTrue();
    }

    [Fact]
    public void ProductOwner_CanRejectWorkItems()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.ProductOwner, Permission.WorkItem_Reject)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public void OnlyOrgAdminAndPO_CanReject(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.WorkItem_Reject)
            .Should().BeFalse($"{role} should NOT have WorkItem_Reject");
    }

    [Fact]
    public void Developer_CanManageLinks()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.Developer, Permission.WorkItem_ManageLinks)
            .Should().BeTrue();
    }

    [Fact]
    public void TeamManager_CanManageTeam()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.TeamManager, Permission.Team_Manage)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectRole.ProductOwner)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public void OnlyOrgAdminAndTeamManager_CanManageTeam(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Team_Manage)
            .Should().BeFalse($"{role} should NOT have Team_Manage");
    }

    // --- Phase 4: Comment permissions ---

    [Theory]
    [InlineData(ProjectRole.OrgAdmin)]
    [InlineData(ProjectRole.ProductOwner)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public void AllRoles_HaveCommentView(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Comment_View)
            .Should().BeTrue($"{role} should have Comment_View");
    }

    [Theory]
    [InlineData(ProjectRole.OrgAdmin)]
    [InlineData(ProjectRole.ProductOwner)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    [InlineData(ProjectRole.Developer)]
    public void AllRolesExceptViewer_HaveCommentCreate(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Comment_Create)
            .Should().BeTrue($"{role} should have Comment_Create");
    }

    [Fact]
    public void Viewer_CannotCreateComments()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.Viewer, Permission.Comment_Create)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(Permission.Comment_EditOwn)]
    [InlineData(Permission.Comment_DeleteOwn)]
    public void Viewer_CannotEditOrDeleteComments(Permission permission)
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.Viewer, permission)
            .Should().BeFalse($"Viewer should NOT have {permission}");
    }

    // --- Phase 4: Planning Poker permissions ---

    [Theory]
    [InlineData(ProjectRole.OrgAdmin)]
    [InlineData(ProjectRole.ProductOwner)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public void AllRoles_HavePokerView(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Poker_View)
            .Should().BeTrue($"{role} should have Poker_View");
    }

    [Fact]
    public void ProductOwner_CannotVoteInPoker()
    {
        PermissionMatrix.RoleHasPermission(ProjectRole.ProductOwner, Permission.Poker_Vote)
            .Should().BeFalse("PO is observer only in poker");
    }

    [Theory]
    [InlineData(ProjectRole.OrgAdmin)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    [InlineData(ProjectRole.Developer)]
    public void DevAndAbove_CanVoteInPoker(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Poker_Vote)
            .Should().BeTrue($"{role} should have Poker_Vote");
    }

    [Theory]
    [InlineData(ProjectRole.Viewer)]
    [InlineData(ProjectRole.ProductOwner)]
    [InlineData(ProjectRole.Developer)]
    public void OnlyLeadership_CanFacilitatePoker(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Poker_Facilitate)
            .Should().BeFalse($"{role} should NOT have Poker_Facilitate");
    }

    [Theory]
    [InlineData(ProjectRole.OrgAdmin)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    public void Leadership_CanFacilitateAndConfirmPoker(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Poker_Facilitate)
            .Should().BeTrue($"{role} should have Poker_Facilitate");
        PermissionMatrix.RoleHasPermission(role, Permission.Poker_ConfirmEstimate)
            .Should().BeTrue($"{role} should have Poker_ConfirmEstimate");
    }

    // --- Phase 4: Notification permissions ---

    [Theory]
    [InlineData(ProjectRole.OrgAdmin)]
    [InlineData(ProjectRole.ProductOwner)]
    [InlineData(ProjectRole.TechnicalLeader)]
    [InlineData(ProjectRole.TeamManager)]
    [InlineData(ProjectRole.Developer)]
    [InlineData(ProjectRole.Viewer)]
    public void AllRoles_HaveNotificationView(ProjectRole role)
    {
        PermissionMatrix.RoleHasPermission(role, Permission.Notification_View)
            .Should().BeTrue($"{role} should have Notification_View");
    }
}
