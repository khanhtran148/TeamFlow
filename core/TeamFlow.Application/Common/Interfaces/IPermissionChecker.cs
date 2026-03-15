namespace TeamFlow.Application.Common.Interfaces;

public interface IPermissionChecker
{
    /// <summary>
    /// Checks if a user has the given permission on a project.
    /// Resolution order: Individual → Team → Organization.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, Guid projectId, Permission permission, CancellationToken ct = default);

    /// <summary>
    /// Gets the effective role for a user on a project.
    /// </summary>
    Task<Domain.Enums.ProjectRole?> GetEffectiveRoleAsync(Guid userId, Guid projectId, CancellationToken ct = default);
}

/// <summary>
/// All granular permissions in the system.
/// </summary>
public enum Permission
{
    // Work Items
    WorkItem_View,
    WorkItem_Create,
    WorkItem_Edit,
    WorkItem_Delete,
    WorkItem_AssignSelf,
    WorkItem_AssignOther,
    WorkItem_ChangeStatus,
    WorkItem_ManageLinks,
    WorkItem_Reject,        // ProductOwner only

    // Sprints
    Sprint_View,
    Sprint_Create,
    Sprint_Start,
    Sprint_Complete,
    Sprint_Edit,

    // Releases
    Release_View,
    Release_Create,
    Release_Edit,
    Release_Publish,

    // Retro
    Retro_View,
    Retro_Facilitate,
    Retro_SubmitCard,
    Retro_Vote,

    // Project
    Project_View,
    Project_Edit,
    Project_ManageMembers,
    Project_Archive,

    // Team/Org
    Team_Manage,
    Org_Admin
}
