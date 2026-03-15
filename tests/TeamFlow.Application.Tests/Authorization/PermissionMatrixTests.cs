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
}
