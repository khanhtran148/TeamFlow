# Phase 2 Summary: Research

## Artifacts
- Full plan at `docs/plans/phase-5-dashboard-notifications/plan.md` (1007 lines)
- State file at `docs/plans/phase-5-dashboard-notifications-implement-state.md`

## Key Codebase Patterns Discovered

### Backend
- **Entity pattern**: Inherit `BaseEntity` (Id, CreatedAt, UpdatedAt) or standalone sealed class with `Guid Id`
- **Handler pattern**: Sealed class with primary constructor, `IRequestHandler<TCmd, Result<TDto>>`, permission check first
- **Error pattern**: `Result.Failure<T>("message")` with string matching in `ApiControllerBase.MapStringError`
- **Repository pattern**: Sealed class with primary constructor taking `TeamFlowDbContext`, `SaveChangesAsync` after mutations
- **Controller pattern**: Sealed class extending `ApiControllerBase`, `[ApiVersion("1.0")]`, `HandleResult(result)`
- **DI**: All registrations in `Infrastructure/DependencyInjection.cs` as `AddScoped`
- **DbContext**: DbSet properties, `ApplyConfigurationsFromAssembly`, soft delete query filter on WorkItems
- **Test pattern**: xUnit + FluentAssertions + NSubstitute, no Arrange/Act/Assert comments, builders with `Bogus`

### Frontend
- **API client**: `apiClient` from `./client`, typed Axios calls, returns `response.data`
- **Hooks**: TanStack Query with `queryKey` factory objects, `useQuery`/`useMutation` with cache invalidation
- **Stores**: Zustand persist pattern (see `auth-store.ts`)
- **Types**: All in `lib/api/types.ts`
- **Pages**: App Router at `app/projects/[projectId]/`
- **Components**: Feature folders under `components/`

### Existing Infrastructure
- `InAppNotification` entity exists (Id, RecipientId, Type, Title, Body, ReferenceId, ReferenceType, IsRead, CreatedAt)
- `IInAppNotificationRepository` exists
- `Permission.Notification_View` exists in the permission enum
- `WorkItemAssignedDomainEvent` exists
- `BurndownDataPoint`, `TeamVelocityHistory`, `SprintSnapshot` entities exist for dashboard queries
- `WorkItemHistory` exists for cycle time calculations
- `search_vector` column on work_items commented out in entity (managed by DB)
- GIN index `idx_wi_search` already exists per plan

## Constraints
- All classes sealed by default
- TFD: failing tests first
- Testcontainers with real PostgreSQL
- CQRS + Result<T> + ProblemDetails
- Permission checks in command handlers via IPermissionChecker
- No Docker for test DB strategy override -- using Testcontainers

## Decisions
- Feature scope: fullstack (backend + frontend)
- Branch: feat/phase-5-dashboard-notifications
- Implementation order: 5.1 -> 5.2 -> 5.3 -> 5.4
