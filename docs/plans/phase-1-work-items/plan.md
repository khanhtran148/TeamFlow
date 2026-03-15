# Plan: Phase 1 -- Work Item Management

## Overview

Phase 1 builds the core work-item engine on top of Phase 0's foundation: project CRUD, the full Epic > Story > Task/Bug/Spike hierarchy, assignment, item linking with circular detection, release basics, backlog/kanban views, and realtime broadcast of all mutations. Auth is not enforced -- seed users only.

## Feature Scope

Scope: **backend-only** (this plan covers API + domain + infrastructure + tests; frontend is a separate plan)

## What Phase 0 Already Provides

- All domain entities: `WorkItem`, `Project`, `Release`, `WorkItemLink`, `WorkItemHistory`, `User`, etc.
- All enums: `WorkItemType`, `WorkItemStatus`, `LinkType`, `LinkScope`, `Priority`, `ReleaseStatus`, `ProjectRole`
- All domain events: `WorkItemCreatedDomainEvent`, `WorkItemStatusChangedDomainEvent`, etc.
- Interfaces: `IWorkItemRepository`, `IHistoryService`, `IBroadcastService`, `IPermissionChecker`, `ICurrentUser`
- Error types: `NotFoundError`, `ForbiddenError`, `ValidationError`, `ConflictError`
- Infrastructure: `TeamFlowDbContext`, `WorkItemRepository` (partial), `HistoryService`, EF Core configurations
- Test infrastructure: `IntegrationTestBase`, builders (`WorkItemBuilder`, `ProjectBuilder`, `UserBuilder`, `OrganizationBuilder`, `SprintBuilder`)
- MediatR pipeline behaviors: `ValidationBehavior`, `LoggingBehavior`
- `PagedResult<T>` model

## Phases

| Phase | Name | Status | Parallelizable | Description |
|-------|------|--------|----------------|-------------|
| 1 | Shared Infrastructure | completed | no | Repository interfaces, base controller, current-user stub, DI wiring |
| 2 | Project CRUD | completed | no | Create, edit, archive, delete, list with filter/search |
| 3 | Work Item CRUD + Hierarchy | completed | no | Full CRUD for all 5 types, parent-child enforcement, soft-delete cascade |
| 4 | Assignment | completed | no | Assign/unassign single assignee, history |
| 5 | Item Linking | completed | no | All 6 link types, bidirectional auto-create, circular detection |
| 6 | Release Basics | completed | no | Release CRUD, assign/unassign items |
| 7 | Backlog + Kanban Queries | completed | no | Hierarchy-grouped backlog, kanban board, filter/search/reorder |
| 8 | Realtime Broadcast | completed | no | MassTransit publish from handlers, SignalR consumer broadcast |
| 9 | Integration Tests | completed | no | End-to-end scenario tests with Testcontainers |

---

## Phase 1: Shared Infrastructure

**Goal:** Fill gaps in Phase 0 plumbing so feature slices have everything they need.

### Tasks

#### 1.1 -- Stub ICurrentUser for Phase 1 (no auth)
- **Size:** S
- **What:** Implement `FakeCurrentUser : ICurrentUser` that returns a fixed seed-user ID. Register as scoped in DI. Replace with real JWT-based implementation in Phase 2.
- **Files to create:**
  - `src/apps/TeamFlow.Api/Services/FakeCurrentUser.cs`
- **Files to modify:**
  - `src/apps/TeamFlow.Api/Program.cs` (register DI)
- **Dependencies:** None
- **Acceptance:** Any handler injecting `ICurrentUser` receives a non-null user ID.

#### 1.2 -- Stub IPermissionChecker for Phase 1 (always allow)
- **Size:** S
- **What:** Implement `AlwaysAllowPermissionChecker : IPermissionChecker` that returns `true` for all checks. Replaced in Phase 2.
- **Files to create:**
  - `src/apps/TeamFlow.Api/Services/AlwaysAllowPermissionChecker.cs`
- **Files to modify:**
  - `src/apps/TeamFlow.Api/Program.cs` (register DI)
- **Dependencies:** None
- **Acceptance:** Permission checks in handlers pass without auth.

#### 1.3 -- ApiControllerBase with HandleResult
- **Size:** S
- **What:** Base controller class with `Sender` property (via `ISender`) and `HandleResult<T>(Result<T>)` that maps error types to ProblemDetails responses.
- **Files to create:**
  - `src/apps/TeamFlow.Api/Controllers/ApiControllerBase.cs`
- **Dependencies:** None
- **Acceptance:** All Result error types map to correct HTTP status codes (400, 403, 404, 409).

#### 1.4 -- Extend repository interfaces
- **Size:** S
- **What:** Add `IProjectRepository`, `IReleaseRepository`, `IWorkItemLinkRepository` interfaces. Extend `IWorkItemRepository` with methods needed by Phase 1 (e.g., `GetByIdWithChildrenAsync`, `SoftDeleteCascadeAsync`).
- **Files to create:**
  - `src/core/TeamFlow.Application/Common/Interfaces/IProjectRepository.cs`
  - `src/core/TeamFlow.Application/Common/Interfaces/IReleaseRepository.cs`
  - `src/core/TeamFlow.Application/Common/Interfaces/IWorkItemLinkRepository.cs`
- **Files to modify:**
  - `src/core/TeamFlow.Application/Common/Interfaces/IWorkItemRepository.cs`
- **Dependencies:** None
- **Acceptance:** All interfaces define methods required by downstream feature slices.

#### 1.5 -- Implement repository classes
- **Size:** M
- **What:** Implement EF Core repositories for Project, Release, WorkItemLink. Extend existing `WorkItemRepository`.
- **Files to create:**
  - `src/core/TeamFlow.Infrastructure/Repositories/ProjectRepository.cs`
  - `src/core/TeamFlow.Infrastructure/Repositories/ReleaseRepository.cs`
  - `src/core/TeamFlow.Infrastructure/Repositories/WorkItemLinkRepository.cs`
- **Files to modify:**
  - `src/core/TeamFlow.Infrastructure/Repositories/WorkItemRepository.cs`
  - `src/core/TeamFlow.Infrastructure/DependencyInjection.cs`
- **Dependencies:** 1.4
- **Acceptance:** All repository methods pass integration tests against Testcontainers PostgreSQL.

#### 1.6 -- Test data builders for new entities
- **Size:** S
- **What:** Add `ReleaseBuilder`, `WorkItemLinkBuilder`, `WorkItemHistoryBuilder`, `ProjectMembershipBuilder`.
- **Files to create:**
  - `tests/TeamFlow.Tests.Common/Builders/ReleaseBuilder.cs`
  - `tests/TeamFlow.Tests.Common/Builders/WorkItemLinkBuilder.cs`
  - `tests/TeamFlow.Tests.Common/Builders/WorkItemHistoryBuilder.cs`
  - `tests/TeamFlow.Tests.Common/Builders/ProjectMembershipBuilder.cs`
- **Dependencies:** None
- **Acceptance:** Builders produce valid entities that persist without constraint violations.

---

## Phase 2: Project CRUD

**Goal:** Full project lifecycle management.

### Tasks

#### 2.1 -- CreateProject (TFD)
- **Size:** M
- **What:** Command + handler + validator + controller endpoint. Creates project in an organization.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Projects/CreateProject/`
- **Files:** `CreateProjectCommand.cs`, `CreateProjectHandler.cs`, `CreateProjectValidator.cs`, `ProjectDto.cs` (shared in `Features/Projects/`)
- **Controller:** `src/apps/TeamFlow.Api/Controllers/ProjectsController.cs`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Projects/CreateProjectTests.cs`
- **Dependencies:** 1.1, 1.2, 1.3, 1.4, 1.5
- **Acceptance:** POST `/api/v1/projects` creates project; validation rejects empty name; history recorded.

#### 2.2 -- UpdateProject (TFD)
- **Size:** S
- **What:** Command to update name, description. Publishes domain event.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Projects/UpdateProject/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Projects/UpdateProjectTests.cs`
- **Dependencies:** 2.1
- **Acceptance:** PUT updates fields; 404 for non-existent project.

#### 2.3 -- ArchiveProject + DeleteProject (TFD)
- **Size:** S
- **What:** Archive sets status to "Archived". Delete is soft-delete. Archived projects excluded from default list.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Projects/ArchiveProject/`, `DeleteProject/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Projects/ArchiveProjectTests.cs`, `DeleteProjectTests.cs`
- **Dependencies:** 2.1
- **Acceptance:** Archive toggles status; delete sets `deleted_at`; both return 404 for missing project.

#### 2.4 -- ListProjects with filter/search (TFD)
- **Size:** M
- **What:** Query with pagination, filter by status (Active/Archived), search by name. Returns `PagedResult<ProjectDto>`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Projects/ListProjects/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Projects/ListProjectsTests.cs`
- **Dependencies:** 2.1
- **Acceptance:** Pagination works; filter by status works; search by name partial match.

#### 2.5 -- GetProject (TFD)
- **Size:** S
- **What:** Query by ID, returns project with summary counts (epic count, open item count).
- **Slice folder:** `src/core/TeamFlow.Application/Features/Projects/GetProject/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Projects/GetProjectTests.cs`
- **Dependencies:** 2.1
- **Acceptance:** Returns project details; 404 for missing.

---

## Phase 3: Work Item CRUD + Hierarchy

**Goal:** Full CRUD for all 5 work item types with parent-child hierarchy enforcement.

### Tasks

#### 3.1 -- CreateWorkItem (TFD)
- **Size:** L
- **What:** Command to create any work item type. Enforces hierarchy rules: Epic has no parent; Story parent must be Epic; Task/Bug/Spike parent must be Story. Sets default status to ToDo. Writes history record via `IHistoryService`. Publishes `WorkItemCreatedDomainEvent`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/CreateWorkItem/`
- **Files:** `CreateWorkItemCommand.cs`, `CreateWorkItemHandler.cs`, `CreateWorkItemValidator.cs`
- **Shared:** `src/core/TeamFlow.Application/Features/WorkItems/WorkItemDto.cs`
- **Controller:** `src/apps/TeamFlow.Api/Controllers/WorkItemsController.cs`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/CreateWorkItemTests.cs`
- **Dependencies:** Phase 2 (project must exist)
- **Hierarchy validation rules:**
  - Epic: `ParentId` must be null
  - UserStory: `ParentId` must reference an Epic
  - Task/Bug/Spike: `ParentId` must reference a UserStory
- **Acceptance:** Creates all 5 types; rejects invalid parent-child combos with clear error; history recorded.

#### 3.2 -- UpdateWorkItem (TFD)
- **Size:** M
- **What:** Update title, description, priority, estimation, acceptance criteria. Each changed field writes a history record with old/new values. Publishes relevant domain events (`PriorityChanged`, etc.).
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/UpdateWorkItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/UpdateWorkItemTests.cs`
- **Dependencies:** 3.1
- **Acceptance:** Updates fields; history records each changed field separately; 404 for missing.

#### 3.3 -- ChangeWorkItemStatus (TFD)
- **Size:** M
- **What:** Dedicated command for status transitions. Validates allowed transitions (see status flow diagram). Publishes `WorkItemStatusChangedDomainEvent`. History records from/to status.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/ChangeStatus/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/ChangeStatusTests.cs`
- **Dependencies:** 3.1
- **Status transitions (Phase 1 -- no role enforcement):**
  - ToDo -> InProgress
  - InProgress -> InReview
  - InReview -> Done
  - Any -> ToDo (regression)
- **Acceptance:** Valid transitions succeed; invalid transitions rejected; history recorded.

#### 3.4 -- GetWorkItem (TFD)
- **Size:** S
- **What:** Query by ID. Returns item with parent info, children summary, link count, assignee, release, sprint references.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/GetWorkItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/GetWorkItemTests.cs`
- **Dependencies:** 3.1
- **Acceptance:** Returns full DTO with related info; 404 for missing; soft-deleted items not returned.

#### 3.5 -- DeleteWorkItem with cascade (TFD)
- **Size:** L
- **What:** Soft-delete work item. Cascades soft-delete to all children recursively. Each deleted item gets a history record. Publishes domain events for each deleted item.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/DeleteWorkItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/DeleteWorkItemTests.cs`
- **Dependencies:** 3.1
- **Special attention:** Recursive cascade -- delete Epic must soft-delete all its Stories and their Tasks/Bugs/Spikes. History preserved for every item in the cascade.
- **Acceptance:** Delete Epic -> all descendants soft-deleted; history written for each; items no longer appear in queries.

#### 3.6 -- MoveWorkItem (reparent) (TFD)
- **Size:** M
- **What:** Change parent of a work item. Validates new hierarchy is legal. Records history (old parent -> new parent).
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/MoveWorkItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/MoveWorkItemTests.cs`
- **Dependencies:** 3.1
- **Acceptance:** Story can move between Epics; Task can move between Stories; cross-type reparenting rejected.

---

## Phase 4: Assignment

**Goal:** Single-assignee assignment with full history tracking.

### Tasks

#### 4.1 -- AssignWorkItem (TFD)
- **Size:** M
- **What:** Command to assign a user to a work item. Validates user exists. Records history (old assignee -> new assignee). Publishes `WorkItemAssignedDomainEvent`. Only Story/Task/Bug/Spike can have assignees (not Epic per features.md).
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/AssignWorkItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/AssignWorkItemTests.cs`
- **Dependencies:** Phase 3
- **Acceptance:** Assigns user; overwrites previous assignee with history; rejects assignment to Epic; 404 for invalid user or item.

#### 4.2 -- UnassignWorkItem (TFD)
- **Size:** S
- **What:** Clears assignee. Records history. Publishes `WorkItemUnassignedDomainEvent`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/UnassignWorkItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/UnassignWorkItemTests.cs`
- **Dependencies:** 4.1
- **Acceptance:** Clears assignee; history records previous; no error if already unassigned.

---

## Phase 5: Item Linking

**Goal:** All 6 link types with bidirectional auto-creation and circular blocking detection.

### Tasks

#### 5.1 -- AddWorkItemLink (TFD)
- **Size:** XL
- **What:** Creates a link between two work items. Auto-creates the reverse link (e.g., A blocks B -> B is-blocked-by A). Records history on both items. Validates items exist. Validates no duplicate link. For `Blocks` and `DependsOn` types: runs circular detection before persisting.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/AddLink/`
- **Controller:** Add endpoints to `WorkItemsController` or new `WorkItemLinksController`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/AddLinkTests.cs`
- **Dependencies:** Phase 3
- **Bidirectional reverse mapping:**
  - Blocks -> IsBlockedBy (store as reverse Blocks with swapped source/target)
  - RelatesTo -> RelatesTo (symmetric)
  - Duplicates -> IsDuplicatedBy (reverse Duplicates)
  - DependsOn -> IsDependencyOf (reverse DependsOn)
  - Causes -> IsCausedBy (reverse Causes)
  - Clones -> IsClonedBy (reverse Clones)
- **Circular detection algorithm:** For Blocks/DependsOn, do a BFS/DFS from target following same link type. If source is reachable, reject with `ConflictError("Circular blocking/dependency detected")`.
- **Cross-project support:** `scope` field set based on whether items share a project.
- **Special attention:** This is the most complex feature in Phase 1. The circular detection must handle chains of arbitrary depth without performance degradation for reasonable graph sizes (<1000 items per project).
- **Acceptance:** Creates forward + reverse link; history on both items; circular block rejected; duplicate link rejected; cross-project scope set correctly.

#### 5.2 -- RemoveWorkItemLink (TFD)
- **Size:** M
- **What:** Removes a link and its reverse. Records history on both items. Publishes `WorkItemLinkRemovedDomainEvent`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/RemoveLink/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/RemoveLinkTests.cs`
- **Dependencies:** 5.1
- **Acceptance:** Both forward and reverse links removed; history on both items; 404 if link doesn't exist.

#### 5.3 -- GetWorkItemLinks (TFD)
- **Size:** S
- **What:** Query all links for a work item, grouped by type and direction. Returns linked item summary (ID, title, type, status).
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/GetLinks/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/GetLinksTests.cs`
- **Dependencies:** 5.1
- **Acceptance:** Returns grouped links with correct direction labels; includes cross-project links.

#### 5.4 -- HasUnresolvedBlockers query (TFD)
- **Size:** S
- **What:** Query that checks if a work item has active (non-Done) blocking links. Used by status change handler to trigger blocked warning data in response.
- **Slice folder:** `src/core/TeamFlow.Application/Features/WorkItems/CheckBlockers/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/WorkItems/CheckBlockersTests.cs`
- **Dependencies:** 5.1
- **Acceptance:** Returns blocker list; empty when all blockers are Done.

---

## Phase 6: Release Basics

**Goal:** Release CRUD and item-to-release assignment.

### Tasks

#### 6.1 -- CreateRelease (TFD)
- **Size:** M
- **What:** Command to create a release within a project. Validates project exists. Publishes `ReleaseCreatedDomainEvent`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Releases/CreateRelease/`
- **Controller:** `src/apps/TeamFlow.Api/Controllers/ReleasesController.cs`
- **Shared:** `src/core/TeamFlow.Application/Features/Releases/ReleaseDto.cs`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Releases/CreateReleaseTests.cs`
- **Dependencies:** Phase 2
- **Acceptance:** Creates release; validation rejects empty name.

#### 6.2 -- UpdateRelease (TFD)
- **Size:** S
- **What:** Update name, description, release date. Cannot update if `NotesLocked`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Releases/UpdateRelease/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Releases/UpdateReleaseTests.cs`
- **Dependencies:** 6.1
- **Acceptance:** Updates fields; rejects update when notes locked; 404 for missing.

#### 6.3 -- DeleteRelease (TFD)
- **Size:** S
- **What:** Delete release. Unlinks all assigned work items (sets their `ReleaseId` to null). Cannot delete a Released release.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Releases/DeleteRelease/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Releases/DeleteReleaseTests.cs`
- **Dependencies:** 6.1
- **Acceptance:** Deletes release; work items unlinked; rejects delete of Released release.

#### 6.4 -- AssignItemToRelease / UnassignItemFromRelease (TFD)
- **Size:** M
- **What:** Assign a work item to a release. Enforces one-release-at-a-time constraint. Records work item history. Publishes `ReleaseItemAssignedDomainEvent`.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Releases/AssignItem/`, `UnassignItem/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Releases/AssignItemTests.cs`
- **Dependencies:** 6.1, Phase 3
- **Acceptance:** Assigns item; rejects if item already in another release (must unassign first); history recorded.

#### 6.5 -- ListReleases + GetRelease (TFD)
- **Size:** S
- **What:** List releases for a project with pagination. Get single release with item count breakdown (by status).
- **Slice folder:** `src/core/TeamFlow.Application/Features/Releases/ListReleases/`, `GetRelease/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Releases/ListReleasesTests.cs`
- **Dependencies:** 6.1
- **Acceptance:** Pagination works; detail includes item counts.

---

## Phase 7: Backlog + Kanban Queries

**Goal:** Read-optimized queries for the two primary views.

### Tasks

#### 7.1 -- GetBacklog (TFD)
- **Size:** L
- **What:** Query returning work items grouped by Epic > Story > children hierarchy. Supports filters: status, priority, assignee, type, sprint, release, "unscheduled" (no release). Supports search by title. Supports reorder (stored as a sort-order field or maintained by client). Returns blocked icon flag, release badge, assignee info.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Backlog/GetBacklog/`
- **Controller:** `src/apps/TeamFlow.Api/Controllers/BacklogController.cs`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Backlog/GetBacklogTests.cs`
- **Dependencies:** Phases 3, 4, 5, 6
- **Performance target:** <500ms for 1000 items.
- **Acceptance:** Returns hierarchy-grouped items; all filters work; blocked items flagged; release badge present.

#### 7.2 -- ReorderBacklog (TFD)
- **Size:** M
- **What:** Command to set sort order for items within a backlog level. Stores order as integer field on work item (add `SortOrder` column if not present).
- **Slice folder:** `src/core/TeamFlow.Application/Features/Backlog/ReorderBacklog/`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Backlog/ReorderBacklogTests.cs`
- **Dependencies:** 7.1
- **Special attention:** May need a migration to add `sort_order INTEGER` to `work_items` if not already present.
- **Acceptance:** Items return in specified order; reorder persists across requests.

#### 7.3 -- GetKanbanBoard (TFD)
- **Size:** L
- **What:** Query returning work items grouped by status column (ToDo, InProgress, InReview, Done). Supports swimlanes by assignee or by epic. Supports filters: assignee, type, priority, sprint, release. Returns blocked icon flag. Scoped to a single project.
- **Slice folder:** `src/core/TeamFlow.Application/Features/Kanban/GetKanbanBoard/`
- **Controller:** `src/apps/TeamFlow.Api/Controllers/KanbanController.cs`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Kanban/GetKanbanBoardTests.cs`
- **Dependencies:** Phases 3, 4, 5
- **Acceptance:** Items grouped by status; swimlanes work; filters work; blocked items flagged.

---

## Phase 8: Realtime Broadcast

**Goal:** All mutations publish domain events through MassTransit to RabbitMQ; a consumer broadcasts via SignalR to connected clients.

### Tasks

#### 8.1 -- MassTransit publish from domain event handlers (TFD)
- **Size:** M
- **What:** Create MediatR notification handlers that publish each domain event to RabbitMQ via MassTransit. One handler per domain event type from Phase 1 event registry.
- **Files to create (one per event):**
  - `src/core/TeamFlow.Application/Features/Events/WorkItemCreatedNotificationHandler.cs`
  - `src/core/TeamFlow.Application/Features/Events/WorkItemStatusChangedNotificationHandler.cs`
  - (etc. for all Phase 1 events)
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Events/` (verify publish called with correct payload)
- **Dependencies:** All previous phases (events are produced by those handlers)
- **Acceptance:** Each mutation triggers a MassTransit publish with correct routing key.

#### 8.2 -- SignalR broadcast consumer
- **Size:** M
- **What:** MassTransit consumer subscribed to `signalr.broadcast` queue. Routes each event to the correct SignalR group (project:{id}) via `IBroadcastService`.
- **Files to create:**
  - `src/apps/TeamFlow.BackgroundServices/Consumers/SignalRBroadcastConsumer.cs`
- **Tests:** `tests/TeamFlow.Application.Tests/Features/Events/SignalRBroadcastConsumerTests.cs`
- **Dependencies:** 8.1
- **Acceptance:** Events published to RabbitMQ reach SignalR clients in the correct group.

#### 8.3 -- DomainEvent store consumer
- **Size:** S
- **What:** MassTransit consumer subscribed to `domain.event.store` queue. Persists every event to the `domain_events` table for the AI event log.
- **Files to create:**
  - `src/apps/TeamFlow.BackgroundServices/Consumers/DomainEventStoreConsumer.cs`
- **Dependencies:** 8.1
- **Acceptance:** Every domain event persisted to `domain_events` with correct partition.

---

## Phase 9: Integration Tests

**Goal:** End-to-end scenario tests proving the full chain works against real PostgreSQL.

### Tasks

#### 9.1 -- Project lifecycle integration test
- **Size:** M
- **What:** Create project -> update -> archive -> list (verify excluded) -> delete.
- **File:** `tests/TeamFlow.Api.Tests/Projects/ProjectLifecycleTests.cs`
- **Dependencies:** Phase 2

#### 9.2 -- Work item hierarchy integration test
- **Size:** L
- **What:** Create Epic -> Stories -> Tasks. Delete Epic -> verify cascade. Move Task between Stories.
- **File:** `tests/TeamFlow.Api.Tests/WorkItems/WorkItemHierarchyTests.cs`
- **Dependencies:** Phase 3

#### 9.3 -- Item linking integration test
- **Size:** L
- **What:** Create link -> verify reverse exists. Attempt circular block -> verify rejection. Remove link -> verify both sides removed. Cross-project link -> verify scope set.
- **File:** `tests/TeamFlow.Api.Tests/WorkItems/ItemLinkingTests.cs`
- **Dependencies:** Phase 5

#### 9.4 -- Release assignment integration test
- **Size:** M
- **What:** Create release -> assign items -> verify one-release constraint -> delete release -> verify items unlinked.
- **File:** `tests/TeamFlow.Api.Tests/Releases/ReleaseAssignmentTests.cs`
- **Dependencies:** Phase 6

---

## Implementation Order (Critical Path)

```
Phase 1 (Shared Infra)
    |
    v
Phase 2 (Project CRUD) -------> Phase 6 (Release) ---> Phase 6.4 (Assign Item)
    |                                                          |
    v                                                          v
Phase 3 (Work Item CRUD) --> Phase 4 (Assignment) -----> Phase 7 (Backlog/Kanban)
    |                                                          |
    v                                                          v
Phase 5 (Item Linking) ---------------------------------> Phase 8 (Realtime)
                                                               |
                                                               v
                                                         Phase 9 (Integration)
```

**Critical path:** Phase 1 -> Phase 2 -> Phase 3 -> Phase 5 -> Phase 7 -> Phase 8 -> Phase 9

Phase 4 (Assignment) and Phase 6 (Release) can proceed in parallel once Phase 3 and Phase 2 are done, respectively.

---

## Risk Areas and Mitigation

| Risk | Severity | Mitigation |
|------|----------|------------|
| Circular link detection performance | High | BFS with visited set; limit traversal depth to 100; index on `source_id` and `target_id` already exist |
| Bidirectional link consistency | High | Wrap forward + reverse creation in a single DB transaction; integration test verifies both exist or neither |
| Soft-delete cascade correctness | Medium | Recursive CTE in repository; test with 3-level hierarchy; verify history count matches deleted count |
| History append-only guarantee | Medium | No EF Core delete/update mappings on `WorkItemHistory`; repository has no Update/Delete methods; integration test verifies |
| Backlog performance at 1000 items | Medium | Use projections + AsNoTracking; eager-load only needed navigations; add composite index if needed; benchmark in integration test |
| sort_order field may not exist | Low | Check schema; add migration if needed before Phase 7 |
| Domain event contract stability | Low | Event records are already defined in Phase 0; use them as-is; no schema changes |

---

## Complexity Summary

| Phase | Task Count | Total Complexity |
|-------|-----------|-----------------|
| 1 - Shared Infrastructure | 6 | 3S + 1M = **M** |
| 2 - Project CRUD | 5 | 3S + 2M = **M** |
| 3 - Work Item CRUD + Hierarchy | 6 | 1S + 3M + 2L = **L** |
| 4 - Assignment | 2 | 1S + 1M = **S** |
| 5 - Item Linking | 4 | 2S + 1M + 1XL = **XL** |
| 6 - Release Basics | 5 | 3S + 2M = **M** |
| 7 - Backlog + Kanban | 3 | 1M + 2L = **L** |
| 8 - Realtime Broadcast | 3 | 1S + 2M = **M** |
| 9 - Integration Tests | 4 | 2M + 2L = **L** |
| **Total** | **38 tasks** | |

---

## TFD Workflow (applies to all tasks)

Every task follows this sequence:
1. **Write failing tests** -- define expected behavior (happy path + validation error + not found)
2. **Implement minimal code** -- make tests pass
3. **Refactor** -- improve while green

Test coverage targets:
- Handlers: 100%
- Validators: 100%
- Domain logic (hierarchy, circular detection): 100%
- Repositories: integration tests via Testcontainers
- Overall Application layer: >=70%

---

## Success Criteria (Phase 1 complete when all true)

- [ ] Full chain: Project -> Epic -> Story -> Task works end-to-end
- [ ] Delete Epic -> all children soft-deleted, history preserved
- [ ] Assign/Unassign displayed correctly in queries
- [ ] Create "A blocks B" -> B shows "is blocked by A" automatically
- [ ] Delete link from A -> reverse disappears from B
- [ ] Circular block attempt -> API returns ConflictError
- [ ] Release badge appears on backlog items after assignment
- [ ] Each endpoint has: happy path + validation error + not found tests
- [ ] No endpoint returns 500 on valid input
- [ ] Two SignalR clients: mutation in one -> other receives event
- [ ] All domain events persisted to `domain_events` table
