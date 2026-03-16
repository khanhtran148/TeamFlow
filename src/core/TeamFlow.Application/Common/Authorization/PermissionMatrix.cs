using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Authorization;

/// <summary>
/// Maps each ProjectRole to its default set of permissions.
/// Based on docs/product/roles-permissions.md.
/// </summary>
public static class PermissionMatrix
{
    private static readonly Dictionary<ProjectRole, HashSet<Permission>> RolePermissions = new()
    {
        [ProjectRole.OrgAdmin] = [.. Enum.GetValues<Permission>()], // All permissions

        [ProjectRole.ProductOwner] =
        [
            Permission.Project_View,
            Permission.Project_Edit,
            Permission.Project_Archive,
            Permission.Project_ManageMembers,
            Permission.WorkItem_View,
            Permission.WorkItem_Create,
            Permission.WorkItem_Edit,
            Permission.WorkItem_Delete,
            Permission.WorkItem_AssignSelf,
            Permission.WorkItem_AssignOther,
            Permission.WorkItem_ChangeStatus,
            Permission.WorkItem_ManageLinks,
            Permission.WorkItem_Reject,
            Permission.Sprint_View,
            Permission.Sprint_Create,
            Permission.Sprint_Edit,
            Permission.Release_View,
            Permission.Release_Create,
            Permission.Release_Edit,
            Permission.Release_Publish,
            Permission.Retro_View,
            Permission.Retro_SubmitCard,
            Permission.Retro_Vote,
            Permission.Comment_View,
            Permission.Comment_Create,
            Permission.Comment_EditOwn,
            Permission.Comment_DeleteOwn,
            Permission.Poker_View,
            // PO: no Poker_Vote (observer only)
            Permission.Notification_View,
        ],

        [ProjectRole.TechnicalLeader] =
        [
            Permission.Project_View,
            Permission.WorkItem_View,
            Permission.WorkItem_Create,
            Permission.WorkItem_Edit,
            Permission.WorkItem_Delete,
            Permission.WorkItem_AssignSelf,
            Permission.WorkItem_AssignOther,
            Permission.WorkItem_ChangeStatus,
            Permission.WorkItem_ManageLinks,
            Permission.Sprint_View,
            Permission.Sprint_Create,
            Permission.Sprint_Edit,
            Permission.Release_View,
            Permission.Release_Create,
            Permission.Release_Edit,
            Permission.Release_Publish,
            Permission.Retro_View,
            Permission.Retro_Facilitate,
            Permission.Retro_SubmitCard,
            Permission.Retro_Vote,
            Permission.Comment_View,
            Permission.Comment_Create,
            Permission.Comment_EditOwn,
            Permission.Comment_DeleteOwn,
            Permission.Poker_View,
            Permission.Poker_Facilitate,
            Permission.Poker_Vote,
            Permission.Poker_ConfirmEstimate,
            Permission.Notification_View,
        ],

        [ProjectRole.TeamManager] =
        [
            Permission.Project_View,
            Permission.WorkItem_View,
            Permission.WorkItem_Create,
            Permission.WorkItem_Edit,
            Permission.WorkItem_Delete,
            Permission.WorkItem_AssignSelf,
            Permission.WorkItem_AssignOther,
            Permission.WorkItem_ChangeStatus,
            Permission.WorkItem_ManageLinks,
            Permission.Sprint_View,
            Permission.Sprint_Create,
            Permission.Sprint_Start,
            Permission.Sprint_Complete,
            Permission.Sprint_Edit,
            Permission.Release_View,
            Permission.Team_Manage,
            Permission.Retro_View,
            Permission.Retro_Facilitate,
            Permission.Retro_SubmitCard,
            Permission.Retro_Vote,
            Permission.Comment_View,
            Permission.Comment_Create,
            Permission.Comment_EditOwn,
            Permission.Comment_DeleteOwn,
            Permission.Poker_View,
            Permission.Poker_Facilitate,
            Permission.Poker_Vote,
            Permission.Poker_ConfirmEstimate,
            Permission.Notification_View,
        ],

        [ProjectRole.Developer] =
        [
            Permission.Project_View,
            Permission.WorkItem_View,
            Permission.WorkItem_Create,
            Permission.WorkItem_Edit,
            Permission.WorkItem_Delete,
            Permission.WorkItem_AssignSelf,
            Permission.WorkItem_AssignOther,
            Permission.WorkItem_ChangeStatus,
            Permission.WorkItem_ManageLinks,
            Permission.Sprint_View,
            Permission.Release_View,
            Permission.Retro_View,
            Permission.Retro_SubmitCard,
            Permission.Retro_Vote,
            Permission.Comment_View,
            Permission.Comment_Create,
            Permission.Comment_EditOwn,
            Permission.Comment_DeleteOwn,
            Permission.Poker_View,
            Permission.Poker_Vote,
            Permission.Notification_View,
        ],

        [ProjectRole.Viewer] =
        [
            Permission.Project_View,
            Permission.WorkItem_View,
            Permission.Sprint_View,
            Permission.Release_View,
            Permission.Retro_View,
            Permission.Comment_View,
            Permission.Poker_View,
            Permission.Notification_View,
        ],
    };

    /// <summary>
    /// Returns true if the given role has the specified permission by default.
    /// </summary>
    public static bool RoleHasPermission(ProjectRole role, Permission permission)
        => RolePermissions.TryGetValue(role, out var perms) && perms.Contains(permission);

    /// <summary>
    /// Returns all permissions for the given role.
    /// </summary>
    public static IReadOnlySet<Permission> GetPermissions(ProjectRole role)
        => RolePermissions.TryGetValue(role, out var perms)
            ? perms
            : new HashSet<Permission>();
}
