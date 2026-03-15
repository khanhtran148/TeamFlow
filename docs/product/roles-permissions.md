# 02 — Roles & Permission System

## 6 Roles — All Per-Project (except Org Admin)

Every role except Org Admin is scoped per-project. The same user can be Tech Lead on Project A and Developer on Project B simultaneously.

| Role | Domain | Key Distinction |
|---|---|---|
| **Org Admin** | Everything | Cannot be restricted by any rule. Typically 1–2 people. |
| **Product Owner** | Product Backlog, Releases, Roadmap | Owns the WHAT. Accepts/Rejects Stories. Cannot start sprints. Does not vote story points. |
| **Technical Leader** | Technical quality, Tasks, Architecture | Owns the HOW. Closes Tasks. Flags Stories as Needs Clarification. Votes story points. Co-owns Releases. |
| **Team Manager** | Sprint lifecycle, Team membership | Creates/starts/closes sprints. Manages team members and roles. Facilitates retros. |
| **Developer** | Task execution | Full CRUD on Tasks/Bugs. Assigns work items. Votes story points. |
| **Viewer** | Read-only | Can view everything. Cannot create, edit, delete, or assign. |

---

## Permission Resolution — 3 Levels

```
Individual override  (highest priority)
        ↓
Team role
        ↓
Organization default  (lowest priority)
```

**Individual override** — per-user, per-project. Set by Org Admin or Team Manager.  
**Team role** — default role for all members of a team on a project.  
**Organization default** — fallback for all members without explicit assignment.

---

## Full Permission Matrix

| Permission | Org Admin | PO | Tech Lead | Team Mgr | Developer | Viewer |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Manage Organization | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Create / Archive Project | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| CRUD Epic | ✅ | ✅ | 👁 | ❌ | ❌ | 👁 |
| CRUD User Story | ✅ | ✅ | 👁 | ❌ | ❌ | 👁 |
| CRUD Task / Bug / Spike | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Set Priority & Reorder Backlog (Epic/Story) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Reorder Task-level items | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Accept User Story | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Reject User Story (mandatory reason) | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Technical Close Task | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Flag Story "Needs Clarification" | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Define Sprint Goal | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Start / Close Sprint | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Manage Team Members & Roles | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Assign / Unassign Work Items | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Vote Story Points (Refinement) | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ |
| Create / Edit / Delete Release | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Assign Items to Release | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Mark Release as Released | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit Release Notes (before ship) | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Add / Remove Item Links | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Override Blocked Item Warning | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ |
| Create Retro Session / Facilitate | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ |
| Submit / Vote Retro Cards | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Create Retro Action Items | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ |
| View everything | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Work Item Status Flow

```
To Do → In Progress → In Review → Done
                           ↓
                  Needs Clarification  →  [PO clarifies]  →  To Do / In Progress
                           
Done / In Review  ←  Rejected  (PO only — mandatory reason required)
```

| Status | Set By |
|---|---|
| To Do | System default on creation |
| In Progress | Developer, Team Manager |
| In Review | Developer |
| Needs Clarification | Technical Leader — notifies PO |
| Done | Tech Lead (Task), PO (Story) |
| Rejected | Product Owner only — reason mandatory |

---

## Implementation Notes for Claude Code

```csharp
// Permission check pattern — use in every handler
public class PermissionChecker
{
    // Resolution order: Individual → Team → Organization
    public async Task<bool> HasPermission(
        Guid userId, Guid projectId, Permission permission)
    {
        // 1. Check individual override
        // 2. Check team role
        // 3. Check org default
        // Return highest-priority matching role's permission
    }
}

// In every command handler that modifies data:
var hasPermission = await _permissionChecker
    .HasPermission(currentUserId, projectId, Permission.WorkItem_Create);
    
if (!hasPermission)
    return Result.Failure<WorkItemDto>(new ForbiddenError());
```

**ProjectRole enum values:**
```csharp
public enum ProjectRole
{
    OrgAdmin,
    ProductOwner,
    TechnicalLeader,
    TeamManager,
    Developer,
    Viewer
}
```

**WorkItemStatus enum values:**
```csharp
public enum WorkItemStatus
{
    ToDo,
    InProgress,
    InReview,
    NeedsСlarification,
    Done,
    Rejected
}
```
