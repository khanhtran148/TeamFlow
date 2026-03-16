# Phase 4 Discovery Context

## What exists already?

### Domain Entities (created in Phase 0, not yet used)
- `RetroSession` — has Id, SprintId?, ProjectId, FacilitatorId, AnonymityMode, Status, AiSummary, Cards nav, ActionItems nav
- `RetroCard` — has Id, SessionId, AuthorId, Category (WentWell/NeedsImprovement/ActionItem), Content, IsDiscussed, Sentiment, ThemeTags, Votes nav
- `RetroVote` — has Id, CardId, VoterId, VoteCount (1-2)
- `RetroActionItem` — has Id, SessionId, CardId?, Title, Description, AssigneeId?, DueDate?, LinkedTaskId?
- `Release` — has Id, ProjectId, Name, Description, ReleaseDate, Status, ReleasedAt, ReleasedById, NotesLocked
- `WorkItem` — has RetroActionItemId field for bidirectional retro link

### Domain Events (defined, not yet consumed)
- `RetroSessionStartedDomainEvent(SessionId, ProjectId, SprintId?, FacilitatorId)`
- `RetroCardSubmittedDomainEvent(SessionId, CardId, AuthorId)`
- `RetroCardsRevealedDomainEvent(SessionId, ProjectId, CardCount, FacilitatorId)`
- `RetroVoteCastDomainEvent(SessionId, CardId, VoterId, VoteCount)`
- `RetroActionItemCreatedDomainEvent(SessionId, ActionItemId, Title, AssigneeId?, ActorId)`
- `RetroSessionClosedDomainEvent(SessionId, ProjectId, SprintId?, CardCount, ActionItemCount, FacilitatorId)`

### Enums
- `RetroSessionStatus`: Draft, Open, Voting, Discussing, Closed
- `RetroCardCategory`: WentWell, NeedsImprovement, ActionItem

### Permissions (already defined)
- `Retro_View` — all roles
- `Retro_Facilitate` — TechnicalLeader, TeamManager, OrgAdmin
- `Retro_SubmitCard` — all except Viewer
- `Retro_Vote` — all except Viewer
- `Release_View`, `Release_Create`, `Release_Edit`, `Release_Publish` — defined with role mapping

### SignalR Infrastructure
- `TeamFlowHub` has `JoinRetroSession` / `LeaveRetroSession` stubs (currently throw — need retro repository)
- `IBroadcastService` has `BroadcastToRetroSessionAsync` already implemented
- `SignalRBroadcastService` implements it via `retro:{sessionId}` group

### Database Schema
- All retro tables exist: `retro_sessions`, `retro_cards`, `retro_votes`, `retro_action_items`
- `releases` table exists with `notes_locked` field
- `work_items.retro_action_item_id` FK exists

### What does NOT exist yet?
- No `Comment` entity or table
- No `PlanningPokerSession` / `PlanningPokerVote` entities or tables
- No `Notification` / `InAppNotification` entity or table
- No retro repositories or handlers
- No comment-related permissions in the enum
- No "ready for sprint" flag on work items
- No release notes field on releases table (only `description`)

## Architecture Patterns Confirmed

### Handler Pattern
- Primary constructor injection
- Permission check first
- Business rule validation
- Entity creation/mutation
- History recording via `IHistoryService`
- Domain event publish via `IPublisher`
- Return `Result<TDto>`

### Controller Pattern
- `ApiControllerBase` with `Sender.Send()` only
- `HandleResult()` for error mapping
- `[ProducesResponseType]` annotations
- Rate limiting via `[EnableRateLimiting]`

### Test Pattern
- xUnit + FluentAssertions + NSubstitute
- Test data builders (`WorkItemBuilder.New().WithProject(...).Build()`)
- `[Theory]` + `[InlineData]` for parameterized tests
- Separate validator tests from handler tests

### Frontend Pattern
- Next.js App Router with `app/projects/[projectId]/...` routing
- Components organized by domain (backlog, kanban, releases, etc.)
- TanStack Query for data fetching
- Zustand for client state
- SignalR client in `lib/signalr/`
- Playwright E2E tests in `e2e/`

## Key Decisions

1. **Comment entity must be created** — new migration required
2. **Planning Poker entities must be created** — new migration required
3. **Notification entity needed for @mention** — new migration required
4. **"Ready for Sprint" needs a field** on WorkItem or a new status — need to decide
5. **Release notes** — use existing `description` field or add a `release_notes TEXT` column?
6. **Retro entities exist but are unsealed** — must seal them per convention
