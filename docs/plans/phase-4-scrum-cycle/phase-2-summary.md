# Phase 2 Research Summary -- Phase 4 Scrum Cycle

## Artifacts
- State file: `docs/plans/phase-4-scrum-cycle-implement-state.md`
- Plan: `docs/plans/phase-4-scrum-cycle/plan.md`
- Discovery: `docs/plans/phase-4-scrum-cycle/discovery-context.md`

## Key Findings

### Existing Infrastructure (from Phase 0-3)
- **513 tests pass** (48 Domain, 298 Application, 25 BackgroundServices, 132 Api, 10 Infrastructure)
- **Retro entities exist**: RetroSession, RetroCard, RetroVote, RetroActionItem (with DB tables/configurations)
- **Retro domain events exist**: 6 events defined in `RetroDomainEvents.cs`
- **Retro permissions exist**: Retro_View, Retro_Facilitate, Retro_SubmitCard, Retro_Vote (in PermissionMatrix)
- **SignalR hub**: Has stubs for JoinRetroSession (throws - needs repository), JoinWorkItem (works)
- **IBroadcastService**: Already supports BroadcastToRetroSessionAsync, BroadcastToWorkItemAsync NOT yet added
- **Release entity**: Has NotesLocked field, but NO ReleaseNotes field yet
- **WorkItem**: Has RetroActionItemId FK, but NO IsReadyForSprint field yet

### What Must Be Created
- Comment entity + table + configuration + repository + migration
- PlanningPokerSession + PlanningPokerVote entities + tables + configurations + repos + migration
- InAppNotification entity + table + configuration + repository + migration
- WorkItem.IsReadyForSprint field + migration column
- Release.ReleaseNotes field + migration column
- Comment/Poker/Notification permissions in Permission enum
- New domain events for Comment and Poker
- BroadcastToWorkItemAsync method on IBroadcastService
- All Application layer features (Comments, Retros, Poker, Backlog, Releases)
- All API controllers
- All frontend components

## Architecture Patterns Confirmed
- **Handler**: Primary constructor, permission first, Result<T> return, IPublisher for events
- **Controller**: ApiControllerBase, Sender.Send() only, HandleResult() for errors
- **Tests**: xUnit + FluentAssertions + NSubstitute, builders with Bogus, [Theory]+[InlineData]
- **Repository**: Sealed class, primary constructor with DbContext, scoped DI
- **Configuration**: IEntityTypeConfiguration<T>, snake_case table names, explicit column mapping
- **Frontend**: apiClient.get/post, TanStack Query hooks, query keys pattern

## Constraints
- All new classes must be sealed
- No business logic in controllers or infrastructure
- Test-First Development mandatory
- Docker Testcontainers for integration tests
- Existing retro entities are NOT sealed (discovery notes this - should seal them)
