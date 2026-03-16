# Phase 4 Implementation Plan -- Full Scrum Cycle

**Date:** 2026-03-16
**Duration:** 4 weeks (Weeks 13-16)
**Scope:** Fullstack -- backend + frontend
**TFD:** Mandatory for all backend work

---

## Sub-Phase Overview

```
4.0  Schema & Shared Infrastructure          (2 days)   — blocks everything
4.1  Comment System                           (5 days)   — parallel with 4.2
4.2  Retrospective                            (6 days)   — parallel with 4.1
4.3  Planning Poker                           (4 days)   — after 4.1 (uses comment patterns)
4.4  Backlog Refinement                       (3 days)   — after 4.0
4.5  Release Detail Page                      (3 days)   — after 4.0
4.6  Integration & E2E                        (2 days)   — after all above
```

**Critical Path:** 4.0 -> 4.2 -> 4.6 (longest chain)

---

## Sub-Phase 4.0 -- Schema & Shared Infrastructure

**Goal:** Database migrations, new entities, new permissions, shared interfaces. Everything downstream depends on this.

### New Domain Entities

#### `Comment`
```csharp
public sealed class Comment : BaseEntity
{
    public Guid WorkItemId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ParentCommentId { get; set; }  // thread/reply
    public string Content { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }     // soft delete

    // Navigation
    public WorkItem? WorkItem { get; set; }
    public User? Author { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
}
```

#### `PlanningPokerSession`
```csharp
public sealed class PlanningPokerSession
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid WorkItemId { get; set; }       // one session per story
    public Guid ProjectId { get; set; }
    public Guid FacilitatorId { get; set; }
    public bool IsRevealed { get; set; } = false;
    public decimal? FinalEstimate { get; set; }
    public Guid? ConfirmedById { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    // Navigation
    public WorkItem? WorkItem { get; set; }
    public Project? Project { get; set; }
    public User? Facilitator { get; set; }
    public User? ConfirmedBy { get; set; }
    public ICollection<PlanningPokerVote> Votes { get; set; } = [];
}
```

#### `PlanningPokerVote`
```csharp
public sealed class PlanningPokerVote
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid VoterId { get; set; }
    public decimal Value { get; set; }          // 1, 2, 3, 5, 8, 13, 21
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PlanningPokerSession? Session { get; set; }
    public User? Voter { get; set; }
}
```

#### `InAppNotification`
```csharp
public sealed class InAppNotification
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;  // "mention", "assignment", etc.
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Guid? ReferenceId { get; set; }             // work item, comment, etc.
    public string? ReferenceType { get; set; }         // "WorkItem", "Comment", etc.
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public User? Recipient { get; set; }
}
```

### Database Migration

New tables:
- `comments` — with indexes on `(work_item_id, created_at DESC)` and `(parent_comment_id)`
- `planning_poker_sessions` — with index on `(work_item_id)` and `UNIQUE (work_item_id)` where `closed_at IS NULL`
- `planning_poker_votes` — with `UNIQUE (session_id, voter_id)`
- `in_app_notifications` — with indexes on `(recipient_id, is_read, created_at DESC)`

Schema additions to existing tables:
- `work_items.is_ready_for_sprint BOOLEAN NOT NULL DEFAULT FALSE`
- `releases.release_notes TEXT` (separate from description)

### New Permissions

Add to `Permission` enum:
```csharp
// Comments
Comment_View,
Comment_Create,
Comment_EditOwn,
Comment_DeleteOwn,

// Planning Poker
Poker_View,
Poker_Facilitate,
Poker_Vote,
Poker_ConfirmEstimate,

// Notifications
Notification_View,
```

Update `PermissionMatrix`:
- All roles get `Comment_View`, `Notification_View`
- All roles except Viewer get `Comment_Create`, `Comment_EditOwn`, `Comment_DeleteOwn`
- All roles get `Poker_View`
- All roles except Viewer and PO get `Poker_Vote`
- PO: no `Poker_Vote` (observer only per requirements)
- TechnicalLeader, TeamManager, OrgAdmin get `Poker_Facilitate`, `Poker_ConfirmEstimate`

### New Repository Interfaces

```
ICommentRepository
IRetroSessionRepository
IPlanningPokerSessionRepository
IInAppNotificationRepository
```

### New Domain Events

```csharp
// Comment events
public sealed record CommentCreatedDomainEvent(
    Guid CommentId, Guid WorkItemId, Guid ProjectId, Guid AuthorId, Guid? ParentCommentId
) : INotification;

public sealed record CommentMentionDomainEvent(
    Guid CommentId, Guid WorkItemId, Guid ProjectId, Guid MentionedUserId, Guid AuthorId
) : INotification;

// Planning Poker events
public sealed record PokerSessionCreatedDomainEvent(
    Guid SessionId, Guid WorkItemId, Guid ProjectId, Guid FacilitatorId
) : INotification;

public sealed record PokerVoteCastDomainEvent(
    Guid SessionId, Guid ProjectId, int TotalVoteCount
) : INotification;

public sealed record PokerVotesRevealedDomainEvent(
    Guid SessionId, Guid ProjectId, Guid FacilitatorId
) : INotification;

public sealed record PokerEstimateConfirmedDomainEvent(
    Guid SessionId, Guid WorkItemId, Guid ProjectId, decimal FinalEstimate, Guid ConfirmedById
) : INotification;
```

### SignalR Hub Updates

Add to `TeamFlowHub`:
- `JoinWorkItem` — already works (validates permission via work item -> project)
- Fix `JoinRetroSession` — inject `IRetroSessionRepository`, resolve projectId, check `Retro_View`

Add to `IBroadcastService`:
- `BroadcastToWorkItemAsync(Guid workItemId, ...)` — for comment/poker events on work item detail page

### FILE OWNERSHIP
```
OWNED BY 4.0:
  src/core/TeamFlow.Domain/Entities/Comment.cs
  src/core/TeamFlow.Domain/Entities/PlanningPokerSession.cs
  src/core/TeamFlow.Domain/Entities/PlanningPokerVote.cs
  src/core/TeamFlow.Domain/Entities/InAppNotification.cs
  src/core/TeamFlow.Domain/Events/CommentDomainEvents.cs
  src/core/TeamFlow.Domain/Events/PokerDomainEvents.cs
  src/core/TeamFlow.Infrastructure/Migrations/XXXXXXXX_Phase4Schema.cs
  src/core/TeamFlow.Infrastructure/Persistence/Configurations/CommentConfiguration.cs
  src/core/TeamFlow.Infrastructure/Persistence/Configurations/PlanningPokerSessionConfiguration.cs
  src/core/TeamFlow.Infrastructure/Persistence/Configurations/PlanningPokerVoteConfiguration.cs
  src/core/TeamFlow.Infrastructure/Persistence/Configurations/InAppNotificationConfiguration.cs
  src/core/TeamFlow.Application/Common/Interfaces/ICommentRepository.cs
  src/core/TeamFlow.Application/Common/Interfaces/IRetroSessionRepository.cs
  src/core/TeamFlow.Application/Common/Interfaces/IPlanningPokerSessionRepository.cs
  src/core/TeamFlow.Application/Common/Interfaces/IInAppNotificationRepository.cs
  src/core/TeamFlow.Infrastructure/Persistence/Repositories/CommentRepository.cs
  src/core/TeamFlow.Infrastructure/Persistence/Repositories/RetroSessionRepository.cs
  src/core/TeamFlow.Infrastructure/Persistence/Repositories/PlanningPokerSessionRepository.cs
  src/core/TeamFlow.Infrastructure/Persistence/Repositories/InAppNotificationRepository.cs

MODIFIED BY 4.0:
  src/core/TeamFlow.Application/Common/Interfaces/IPermissionChecker.cs (Permission enum)
  src/core/TeamFlow.Application/Common/Authorization/PermissionMatrix.cs
  src/core/TeamFlow.Application/Common/Interfaces/IBroadcastService.cs
  src/apps/TeamFlow.Api/Services/SignalRBroadcastService.cs
  src/apps/TeamFlow.Api/Hubs/TeamFlowHub.cs
  src/core/TeamFlow.Domain/Entities/WorkItem.cs (IsReadyForSprint)
  src/core/TeamFlow.Domain/Entities/Release.cs (ReleaseNotes)
  tests/TeamFlow.Tests.Common/Builders/ (new builders)
  tests/TeamFlow.Domain.Tests/EnumTests.cs (new enums)
```

### Tests (TFD)
- Domain enum tests for new permissions
- PermissionMatrix tests verifying new permission/role mappings
- Test data builders: `CommentBuilder`, `RetroSessionBuilder`, `PlanningPokerSessionBuilder`, `InAppNotificationBuilder`

---

## Sub-Phase 4.1 -- Comment System

**Goal:** Comment on work items with threads, @mention notifications, edit/delete own, realtime.
**Depends on:** 4.0
**PARALLEL: yes (with 4.2, 4.4, 4.5)**

### Application Layer -- Commands & Queries

```
Features/Comments/
  CreateComment/
    CreateCommentCommand.cs      — WorkItemId, Content, ParentCommentId?
    CreateCommentHandler.cs      — permission check, parse @mentions, persist, publish events
    CreateCommentValidator.cs    — Content not empty, max 10000 chars
  UpdateComment/
    UpdateCommentCommand.cs      — CommentId, Content
    UpdateCommentHandler.cs      — only own comment, not deleted
    UpdateCommentValidator.cs
  DeleteComment/
    DeleteCommentCommand.cs      — CommentId
    DeleteCommentHandler.cs      — only own comment, soft delete
  GetComments/
    GetCommentsQuery.cs          — WorkItemId, Page, PageSize
    GetCommentsHandler.cs        — paginated, threaded (top-level + replies)
  CommentDto.cs
```

### @Mention Parsing

- Parse `@username` from comment content (regex: `@([a-zA-Z0-9._-]+)`)
- Resolve usernames to user IDs via `IUserRepository.GetByNamesAsync(names)`
- For each mentioned user: publish `CommentMentionDomainEvent`
- MediatR notification handler creates `InAppNotification` for each mentioned user
- Broadcast notification to `user:{userId}` via SignalR

### API Endpoints

```
POST   /api/v1/workitems/{id}/comments          — create comment
GET    /api/v1/workitems/{id}/comments           — list comments (paginated)
PUT    /api/v1/comments/{id}                     — edit own comment
DELETE /api/v1/comments/{id}                     — soft delete own comment
```

### SignalR Events

- `comment.created` -> broadcast to `workitem:{workItemId}` group
- `comment.updated` -> broadcast to `workitem:{workItemId}` group
- `comment.deleted` -> broadcast to `workitem:{workItemId}` group
- `notification.created` -> broadcast to `user:{userId}` group

### Frontend

**Pages:**
- Work Item Detail page: add Comments tab/section below description

**Components:**
```
components/comments/
  comment-list.tsx          — paginated list with load-more
  comment-item.tsx          — single comment with avatar, time, edit/delete actions
  comment-form.tsx          — textarea with @mention autocomplete
  comment-thread.tsx        — parent + nested replies
  mention-autocomplete.tsx  — dropdown matching project members
```

**Hooks/API:**
```
lib/api/comments.ts         — API client functions
lib/hooks/use-comments.ts   — TanStack Query hooks
```

### FILE OWNERSHIP
```
OWNED BY 4.1:
  src/core/TeamFlow.Application/Features/Comments/**
  src/apps/TeamFlow.Api/Controllers/CommentsController.cs
  src/apps/teamflow-web/components/comments/**
  src/apps/teamflow-web/lib/api/comments.ts
  src/apps/teamflow-web/lib/hooks/use-comments.ts
  tests/TeamFlow.Application.Tests/Features/Comments/**
  tests/TeamFlow.Api.Tests/Comments/**
  e2e/comments/**

MODIFIED BY 4.1:
  src/apps/TeamFlow.Api/Controllers/WorkItemsController.cs (comments endpoints)
  src/apps/teamflow-web/app/projects/[projectId]/work-items/[workItemId]/page.tsx
```

### Tests (TFD)

**Unit tests (Application.Tests):**
- CreateComment: happy path, empty content, too long content, invalid work item, permission denied, parent comment not found, @mention parsing
- UpdateComment: happy path, not own comment, deleted comment, not found
- DeleteComment: happy path, not own comment, already deleted
- GetComments: paginated, empty, threaded structure

**API Integration tests:**
- POST /comments returns 201
- GET /comments returns threaded comments
- PUT /comments/{id} by non-author returns 403
- DELETE /comments/{id} soft deletes

**E2E:**
- Add comment on work item detail -> visible without refresh
- @mention -> notification appears for mentioned user

---

## Sub-Phase 4.2 -- Retrospective

**Goal:** Full retro lifecycle with anonymous mode, dot voting, action items, previous session display, auto summary.
**Depends on:** 4.0
**PARALLEL: yes (with 4.1, 4.4, 4.5)**

### Application Layer -- Commands & Queries

```
Features/Retros/
  CreateRetroSession/
    CreateRetroSessionCommand.cs    — ProjectId, SprintId?, AnonymityMode
    CreateRetroSessionHandler.cs
    CreateRetroSessionValidator.cs
  StartRetroSession/
    StartRetroSessionCommand.cs     — SessionId
    StartRetroSessionHandler.cs     — transitions Draft -> Open, locks anonymity
  TransitionRetroSession/
    TransitionRetroSessionCommand.cs — SessionId, TargetStatus
    TransitionRetroSessionHandler.cs — Open->Voting->Discussing->Closed, facilitator only
  SubmitRetroCard/
    SubmitRetroCardCommand.cs       — SessionId, Category, Content
    SubmitRetroCardHandler.cs       — only when Open, permission check
    SubmitRetroCardValidator.cs
  CastRetroVote/
    CastRetroVoteCommand.cs         — CardId, VoteCount (1 or 2)
    CastRetroVoteHandler.cs         — only when Voting, max 5 total votes per session, max 2 per card
  MarkCardDiscussed/
    MarkCardDiscussedCommand.cs     — CardId
    MarkCardDiscussedHandler.cs     — facilitator only, only when Discussing
  CreateRetroActionItem/
    CreateRetroActionItemCommand.cs — SessionId, CardId?, Title, Description?, AssigneeId?, DueDate?, LinkToBacklog?
    CreateRetroActionItemHandler.cs — creates action item, optionally creates linked Task
    CreateRetroActionItemValidator.cs
  CloseRetroSession/
    CloseRetroSessionCommand.cs     — SessionId
    CloseRetroSessionHandler.cs     — generates summary, publishes event
  GetRetroSession/
    GetRetroSessionQuery.cs         — SessionId
    GetRetroSessionHandler.cs       — includes cards (anonymous: strip AuthorId), votes, action items
  GetPreviousActionItems/
    GetPreviousActionItemsQuery.cs  — ProjectId
    GetPreviousActionItemsHandler.cs — returns action items from most recent closed session
  ListRetroSessions/
    ListRetroSessionsQuery.cs       — ProjectId, Page, PageSize
    ListRetroSessionsHandler.cs
  RetroSessionDto.cs
  RetroCardDto.cs
  RetroActionItemDto.cs
```

### Retro Session State Machine

```
Draft -> Open           (facilitator: StartRetroSession)
                        ACTION: locks AnonymityMode
Open -> Voting          (facilitator: TransitionRetroSession)
Voting -> Discussing    (facilitator: TransitionRetroSession)
Discussing -> Closed    (facilitator: CloseRetroSession)
                        ACTION: generates summary, publishes RetroSessionClosedDomainEvent
```

Invalid transitions return `Result.Failure` with descriptive error.

### Anonymity Enforcement

- When `AnonymityMode == "Anonymous"` AND session status >= Open:
  - `GetRetroSession` handler strips `AuthorId` from all `RetroCardDto` responses
  - No endpoint exposes card authors
  - Database still stores `AuthorId` (for analytics/audit)
  - `AnonymityMode` cannot change after session opens

### Dot Voting Rules
- 5 total votes per member per session
- Max 2 votes on a single card (via `VoteCount` field)
- Handler tracks running total via `SUM(vote_count) WHERE voter_id = X AND card.session_id = Y`

### Action Item -> Backlog Link
- When `LinkToBacklog == true` in `CreateRetroActionItemCommand`:
  - Create a `WorkItem` of type `Task` with title from action item
  - Set `WorkItem.RetroActionItemId` to the action item ID
  - Set custom field tag: `retro-action`
  - Set `RetroActionItem.LinkedTaskId` to the new work item ID

### Auto-Generated Summary
- On close: compute card count by category, top-voted cards, action item count
- Store as JSON in `RetroSession.AiSummary` (structure matches `Retro.SessionClosed` event payload)
- Available to all roles including Viewer after close

### API Endpoints

```
POST   /api/v1/retros                              — create session
GET    /api/v1/retros/{id}                          — get session with cards/votes/actions
GET    /api/v1/retros                               — list sessions for project
POST   /api/v1/retros/{id}/start                    — Draft -> Open
POST   /api/v1/retros/{id}/transition               — Open->Voting, Voting->Discussing
POST   /api/v1/retros/{id}/close                    — Discussing -> Closed
POST   /api/v1/retros/{id}/cards                    — submit card
POST   /api/v1/retros/{id}/cards/{cardId}/vote      — cast vote
POST   /api/v1/retros/{id}/cards/{cardId}/discussed — mark as discussed
POST   /api/v1/retros/{id}/action-items             — create action item
GET    /api/v1/retros/previous-actions               — get previous session action items
```

### SignalR Events (broadcast to `retro:{sessionId}`)

- `retro.session_started` — includes session DTO
- `retro.card_submitted` — card count only (not content, to prevent anchoring before reveal)
- `retro.cards_revealed` — full card list (triggers when moving to Voting)
- `retro.vote_cast` — updated vote totals per card
- `retro.card_discussed` — card ID marked discussed
- `retro.action_item_created` — action item DTO
- `retro.session_closed` — summary DTO

### Frontend

**Pages:**
```
app/projects/[projectId]/retros/page.tsx              — list sessions
app/projects/[projectId]/retros/[retroId]/page.tsx    — session detail (main retro board)
```

**Components:**
```
components/retros/
  retro-session-list.tsx       — list of sessions with status badges
  retro-board.tsx              — main board with 3 columns (WentWell, NeedsImprovement, ActionItem)
  retro-card.tsx               — individual card (anonymous: no author)
  retro-card-form.tsx          — submit card form
  retro-vote-controls.tsx      — dot voting UI (remaining votes counter)
  retro-action-item-form.tsx   — create action item with optional backlog link
  retro-action-item-list.tsx   — action items list
  retro-previous-actions.tsx   — previous session action items banner
  retro-summary.tsx            — post-close summary view
  retro-session-controls.tsx   — facilitator controls (start, transition, close)
```

### FILE OWNERSHIP
```
OWNED BY 4.2:
  src/core/TeamFlow.Application/Features/Retros/**
  src/apps/TeamFlow.Api/Controllers/RetrosController.cs
  src/apps/teamflow-web/app/projects/[projectId]/retros/**
  src/apps/teamflow-web/components/retros/**
  src/apps/teamflow-web/lib/api/retros.ts
  src/apps/teamflow-web/lib/hooks/use-retros.ts
  tests/TeamFlow.Application.Tests/Features/Retros/**
  tests/TeamFlow.Api.Tests/Retros/**
  e2e/retros/**
```

### Tests (TFD)

**Unit tests (Application.Tests):**
- CreateRetroSession: happy path, invalid project, permission denied (Viewer, Developer)
- StartRetroSession: happy path, not Draft, not facilitator
- TransitionRetroSession: valid transitions, invalid transitions, not facilitator
- SubmitRetroCard: happy path, session not Open, Viewer denied, content validation
- CastRetroVote: happy path, session not Voting, exceed 5 votes, exceed 2 per card, duplicate voter on same card
- CreateRetroActionItem: happy path, with backlog link creates Task, without link
- CloseRetroSession: generates summary, not Discussing, not facilitator
- GetRetroSession: anonymous mode strips author, public mode includes author
- GetPreviousActionItems: returns from last closed session, empty when no previous

**API Integration tests:**
- Full lifecycle: create -> start -> submit cards -> transition to voting -> vote -> transition to discussing -> close
- Anonymous session: verify no author info in response
- Permission matrix: Viewer can read but not submit

**E2E:**
- Retro board: submit card appears for all participants (realtime)
- Vote: count updates live
- Close: summary visible to Viewer

---

## Sub-Phase 4.3 -- Planning Poker

**Goal:** Fibonacci estimation sessions per user story with hidden votes, facilitator reveal, and confirmation.
**Depends on:** 4.0
**Recommended after:** 4.1 (shares realtime patterns)

### Application Layer

```
Features/PlanningPoker/
  CreatePokerSession/
    CreatePokerSessionCommand.cs    — WorkItemId
    CreatePokerSessionHandler.cs    — one active session per work item
    CreatePokerSessionValidator.cs
  CastPokerVote/
    CastPokerVoteCommand.cs         — SessionId, Value (1,2,3,5,8,13,21)
    CastPokerVoteHandler.cs         — PO cannot vote, update existing vote
    CastPokerVoteValidator.cs       — value must be in Fibonacci set
  RevealPokerVotes/
    RevealPokerVotesCommand.cs      — SessionId
    RevealPokerVotesHandler.cs      — facilitator only, sets IsRevealed = true
  ConfirmPokerEstimate/
    ConfirmPokerEstimateCommand.cs  — SessionId, FinalEstimate
    ConfirmPokerEstimateHandler.cs  — TL/TM/Admin only, writes to WorkItem.EstimationValue
  GetPokerSession/
    GetPokerSessionQuery.cs         — SessionId or WorkItemId
    GetPokerSessionHandler.cs       — votes hidden until revealed (returns count only)
  PokerSessionDto.cs
  PokerVoteDto.cs
```

### Vote Visibility Rules
- Before reveal: return vote count only (not individual values)
- After reveal: return all votes with voter name and value
- PO sees the board but has no vote button (enforced by `Poker_Vote` permission)

### API Endpoints

```
POST   /api/v1/poker                             — create session
GET    /api/v1/poker/{id}                         — get session (votes hidden/revealed)
GET    /api/v1/poker/by-workitem/{workItemId}     — get active session for work item
POST   /api/v1/poker/{id}/vote                    — cast/update vote
POST   /api/v1/poker/{id}/reveal                  — reveal all votes
POST   /api/v1/poker/{id}/confirm                 — confirm final estimate
```

### SignalR Events (broadcast to `project:{projectId}`)

- `poker.session_created` — session DTO
- `poker.vote_cast` — total vote count only (not values)
- `poker.votes_revealed` — full vote details
- `poker.estimate_confirmed` — final value, updates work item

### Frontend

**Components:**
```
components/poker/
  poker-session.tsx           — main poker board with card fan
  poker-card.tsx              — individual Fibonacci card (selectable)
  poker-vote-summary.tsx      — before reveal: count; after: all votes
  poker-controls.tsx          — facilitator: reveal, confirm buttons
  poker-result.tsx            — final estimate display
```

Integration: poker session accessible from Work Item Detail page for UserStory type items.

### FILE OWNERSHIP
```
OWNED BY 4.3:
  src/core/TeamFlow.Application/Features/PlanningPoker/**
  src/apps/TeamFlow.Api/Controllers/PlanningPokerController.cs
  src/apps/teamflow-web/components/poker/**
  src/apps/teamflow-web/lib/api/poker.ts
  src/apps/teamflow-web/lib/hooks/use-poker.ts
  tests/TeamFlow.Application.Tests/Features/PlanningPoker/**
  tests/TeamFlow.Api.Tests/PlanningPoker/**
  e2e/poker/**
```

### Tests (TFD)

**Unit tests:**
- CreatePokerSession: happy path, duplicate active session for same work item, non-story item
- CastPokerVote: happy path, PO returns forbidden, invalid Fibonacci value, update existing vote
- RevealPokerVotes: happy path, not facilitator, already revealed
- ConfirmPokerEstimate: happy path, not TL/TM, writes estimation to work item, records history
- GetPokerSession: votes hidden before reveal, votes visible after reveal

**API Integration tests:**
- Full flow: create -> vote -> reveal -> confirm -> work item updated

**E2E:**
- PO sees session but no vote button
- Vote count updates live
- Reveal shows all votes simultaneously

---

## Sub-Phase 4.4 -- Backlog Refinement

**Goal:** Mark items "Ready for Sprint", bulk priority update, blocked/ready filters.
**Depends on:** 4.0
**PARALLEL: yes (with 4.1, 4.2)**

### Application Layer

```
Features/Backlog/
  MarkReadyForSprint/
    MarkReadyForSprintCommand.cs    — WorkItemId, IsReady
    MarkReadyForSprintHandler.cs    — permission check, toggle flag, history
  BulkUpdatePriority/
    BulkUpdatePriorityCommand.cs    — Items: [{WorkItemId, Priority}]
    BulkUpdatePriorityHandler.cs    — permission check per item, batch update
    BulkUpdatePriorityValidator.cs
```

### Backlog Query Enhancements

Modify existing `GetBacklogQuery` to support:
- `isReady` filter (true/false)
- `isBlocked` filter (true/false) -- uses existing blocker check logic

### API Endpoints

```
POST   /api/v1/workitems/{id}/ready       — toggle ready for sprint
POST   /api/v1/backlog/bulk-priority       — bulk update priority
```

Existing `GET /api/v1/backlog` gains `isReady` and `isBlocked` query params.

### Frontend

**Components:**
```
components/backlog/
  ready-badge.tsx              — "Ready" pill badge on backlog items
  bulk-priority-dialog.tsx     — multi-select + priority dropdown
  backlog-filters.tsx          — add "Ready only" / "Blocked only" filter toggles
```

### FILE OWNERSHIP
```
OWNED BY 4.4:
  src/core/TeamFlow.Application/Features/Backlog/MarkReadyForSprint/**
  src/core/TeamFlow.Application/Features/Backlog/BulkUpdatePriority/**
  src/apps/teamflow-web/components/backlog/ready-badge.tsx
  src/apps/teamflow-web/components/backlog/bulk-priority-dialog.tsx
  tests/TeamFlow.Application.Tests/Features/Backlog/MarkReadyTests.cs
  tests/TeamFlow.Application.Tests/Features/Backlog/BulkUpdatePriorityTests.cs
  e2e/backlog/refinement.spec.ts

MODIFIED BY 4.4:
  src/core/TeamFlow.Application/Features/Backlog/GetBacklog/GetBacklogQuery.cs
  src/core/TeamFlow.Application/Features/Backlog/GetBacklog/GetBacklogHandler.cs
  src/apps/teamflow-web/components/backlog/backlog-filters.tsx
  src/apps/TeamFlow.Api/Controllers/BacklogController.cs
  src/apps/TeamFlow.Api/Controllers/WorkItemsController.cs
```

### Tests (TFD)

**Unit tests:**
- MarkReadyForSprint: happy path, Viewer denied, non-existent item, records history
- BulkUpdatePriority: happy path (3 items), partial permission failure, empty list, validates priority enum
- GetBacklog with isReady filter, with isBlocked filter

---

## Sub-Phase 4.5 -- Release Detail Page

**Goal:** Progress tracking, grouped views, editable release notes, confirm dialog for releasing with open items.
**Depends on:** 4.0
**PARALLEL: yes (with 4.1, 4.2)**

### Application Layer

```
Features/Releases/
  GetReleaseDetail/
    GetReleaseDetailQuery.cs     — ReleaseId
    GetReleaseDetailHandler.cs   — progress counts, grouped items
  UpdateReleaseNotes/
    UpdateReleaseNotesCommand.cs — ReleaseId, Notes
    UpdateReleaseNotesHandler.cs — PO/TL only, not after ship
  ShipRelease/
    ShipReleaseCommand.cs        — ReleaseId, ConfirmOpenItems (bool)
    ShipReleaseHandler.cs        — if open items and !ConfirmOpenItems: return incomplete list
  ReleaseDetailDto.cs            — includes progress, grouped items, notes
```

### Release Detail DTO Structure

```csharp
public sealed record ReleaseDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? ReleaseNotes,
    DateOnly? ReleaseDate,
    ReleaseStatus Status,
    bool NotesLocked,
    bool IsOverdue,
    ReleaseProgressDto Progress,
    IReadOnlyList<ReleaseGroupDto> ByEpic,
    IReadOnlyList<ReleaseGroupDto> ByAssignee,
    IReadOnlyList<ReleaseGroupDto> BySprint,
    DateTime CreatedAt
);

public sealed record ReleaseProgressDto(
    int TotalItems, int DoneItems, int InProgressItems, int ToDoItems,
    decimal TotalPoints, decimal DonePoints, decimal InProgressPoints, decimal ToDoPoints
);
```

### Ship Release Flow
1. Client calls `POST /api/v1/releases/{id}/ship` with `confirmOpenItems: false`
2. If open items exist: return 409 with list of incomplete items
3. Client shows confirm dialog with the list
4. Client re-calls with `confirmOpenItems: true`
5. Handler sets `Status = Released`, `ReleasedAt = now`, `NotesLocked = true`

### API Endpoints

```
GET    /api/v1/releases/{id}/detail          — full detail with groupings
PUT    /api/v1/releases/{id}/notes           — update release notes
POST   /api/v1/releases/{id}/ship            — ship release (with confirm flow)
```

### Frontend

**Pages:**
Enhance existing `app/projects/[projectId]/releases/[releaseId]/page.tsx`

**Components:**
```
components/releases/
  release-progress-bar.tsx      — stacked bar (Done/InProgress/ToDo) with counts + points
  release-grouped-view.tsx      — tab group: by Epic, by Assignee, by Sprint
  release-group-section.tsx     — collapsible group with items
  release-notes-editor.tsx      — rich text editor for notes (locked after ship)
  release-ship-dialog.tsx       — confirm dialog listing incomplete items
  release-overdue-badge.tsx     — red "Overdue" indicator
```

### FILE OWNERSHIP
```
OWNED BY 4.5:
  src/core/TeamFlow.Application/Features/Releases/GetReleaseDetail/**
  src/core/TeamFlow.Application/Features/Releases/UpdateReleaseNotes/**
  src/core/TeamFlow.Application/Features/Releases/ShipRelease/**
  src/core/TeamFlow.Application/Features/Releases/ReleaseDetailDto.cs
  src/apps/teamflow-web/components/releases/release-progress-bar.tsx
  src/apps/teamflow-web/components/releases/release-grouped-view.tsx
  src/apps/teamflow-web/components/releases/release-group-section.tsx
  src/apps/teamflow-web/components/releases/release-notes-editor.tsx
  src/apps/teamflow-web/components/releases/release-ship-dialog.tsx
  src/apps/teamflow-web/components/releases/release-overdue-badge.tsx
  tests/TeamFlow.Application.Tests/Features/Releases/GetReleaseDetailTests.cs
  tests/TeamFlow.Application.Tests/Features/Releases/UpdateReleaseNotesTests.cs
  tests/TeamFlow.Application.Tests/Features/Releases/ShipReleaseTests.cs
  tests/TeamFlow.Api.Tests/Releases/ReleaseDetailTests.cs
  e2e/releases/release-detail.spec.ts

MODIFIED BY 4.5:
  src/apps/TeamFlow.Api/Controllers/ReleasesController.cs
  src/apps/teamflow-web/app/projects/[projectId]/releases/[releaseId]/page.tsx
```

### Tests (TFD)

**Unit tests:**
- GetReleaseDetail: correct progress counts, grouped by epic/assignee/sprint, overdue flag
- UpdateReleaseNotes: happy path, notes locked after ship, Viewer denied, Developer denied
- ShipRelease: no open items ships immediately, open items without confirm returns 409 with list, open items with confirm ships, already released error

**API Integration tests:**
- GET /releases/{id}/detail returns grouped items
- Ship flow: 409 -> confirm -> 200

**E2E:**
- Release detail page shows progress bar
- Ship with open items shows confirm dialog
- Release notes editable before ship, locked after

---

## Sub-Phase 4.6 -- Integration & E2E

**Goal:** Cross-feature integration tests, performance regression check, final polish.
**Depends on:** 4.1, 4.2, 4.3, 4.4, 4.5

### Scope

1. **Cross-feature E2E tests:**
   - Create retro -> create action item -> verify Task appears in backlog with `retro-action` tag
   - Planning poker -> confirm estimate -> verify work item estimation updated
   - Comment with @mention -> verify notification delivered
   - Release detail -> ship with open items -> confirm dialog

2. **Performance regression:**
   - Backlog 1000 items: <500ms with new filters
   - No endpoint >1 second
   - SignalR broadcast latency <2 seconds

3. **Frontend polish:**
   - Navigation: add Retro and Poker links to project sidebar
   - Notification bell icon in topbar
   - Dark/light mode works on all new pages

4. **Permission matrix validation:**
   - Automated test: every new endpoint checked against all 6 roles

### FILE OWNERSHIP
```
OWNED BY 4.6:
  e2e/integration/cross-feature.spec.ts
  e2e/integration/performance.spec.ts
  tests/TeamFlow.Api.Tests/Phase4/PermissionMatrixTests.cs

MODIFIED BY 4.6:
  src/apps/teamflow-web/components/layout/ (sidebar links)
  src/apps/teamflow-web/components/layout/ (notification bell)
```

---

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| Retro state machine complexity | Medium | Strict state transition tests with every invalid path covered |
| Anonymity leak in retro | High | Handler-level enforcement; no DB query joins author in anonymous mode; E2E test verifies |
| Planning Poker vote timing | Medium | SignalR broadcast vote count only; values sent only on reveal |
| @mention notification spam | Low | Rate limit on comment creation; batch mention processing |
| Migration conflicts with Phase 3 late fixes | Medium | Run Phase 4 migration after all Phase 3 branches merged |
| Release ship with concurrent changes | Medium | Use optimistic concurrency on Release entity |
| Performance regression from new tables | Low | Add indexes upfront in 4.0 migration; test with 1000-item backlog |

---

## Test Strategy

### Test-First Development Workflow

Every feature follows:
1. Write failing unit tests for the handler
2. Write failing validator tests
3. Implement handler + validator until green
4. Write API integration test
5. Implement controller endpoint until green
6. Write E2E test
7. Implement frontend until green
8. Refactor while green

### Test Categories per Sub-Phase

| Sub-Phase | Unit Tests | Integration Tests | E2E Tests |
|---|---|---|---|
| 4.0 | ~15 (builders, enums, permissions) | ~5 (migration, repos) | 0 |
| 4.1 | ~20 (comment CRUD, mentions) | ~8 (API endpoints) | ~3 |
| 4.2 | ~30 (retro lifecycle, voting, anonymity) | ~10 (API endpoints) | ~5 |
| 4.3 | ~15 (poker flow, vote hiding) | ~6 (API endpoints) | ~3 |
| 4.4 | ~10 (ready flag, bulk priority) | ~4 (API endpoints) | ~2 |
| 4.5 | ~12 (release detail, ship flow) | ~5 (API endpoints) | ~3 |
| 4.6 | ~5 (permission matrix) | 0 | ~5 (cross-feature) |
| **Total** | **~107** | **~38** | **~21** |

### Coverage Target
- Application layer: >= 70%
- New features: >= 80%

---

## Dependencies Summary

```
4.0 ──┬── 4.1 (Comments)      ──┐
      ├── 4.2 (Retro)          ──┤
      ├── 4.4 (Refinement)     ──┼── 4.6 (Integration)
      └── 4.5 (Release Detail) ──┤
           4.1 ── 4.3 (Poker)  ──┘
```

- 4.0 blocks all
- 4.1, 4.2, 4.4, 4.5 can run in parallel after 4.0
- 4.3 recommended after 4.1 (shared realtime patterns, not a hard block)
- 4.6 after everything

---

## Implementation Order (Recommended)

**Week 1:** 4.0 (2 days) + start 4.1 + start 4.2
**Week 2:** Complete 4.1 + 4.2 continues + start 4.4 + start 4.5
**Week 3:** Complete 4.2 + 4.3 + complete 4.4 + complete 4.5
**Week 4:** Complete 4.3 + 4.6 integration + bug fixes + polish

---

## Planning Notes

### Codebase Scout Findings
- Retro entities (RetroSession, RetroCard, RetroVote, RetroActionItem) exist but have no Application/API layer
- SignalR hub has retro stubs that need real implementation
- `IBroadcastService` already supports retro and work item groups
- Permission matrix already has Retro permissions defined
- No Comment or PlanningPoker entities exist -- must create from scratch
- Frontend has established patterns: components by domain, TanStack Query hooks, Playwright E2E
- 290+ existing tests provide patterns to follow
- WorkItem already has `RetroActionItemId` FK for bidirectional retro link

### Key Decisions
- "Ready for Sprint" implemented as a boolean field on WorkItem (not a new status), to avoid complicating the status state machine
- Release notes stored as separate `release_notes` column (not overloading `description`)
- PlanningPokerSession has unique constraint per work item (only one active session at a time)
- Comment threading is one level deep (parent + replies, no nested replies)
- @mention resolution happens in the command handler at persist time, not lazily

## Approval

**Does this plan look good? I can adjust scope, ordering, or entity design before we proceed to implementation.**
