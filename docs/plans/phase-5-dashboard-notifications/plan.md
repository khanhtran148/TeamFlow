# Phase 5 Plan: Insights & Automation

**Created:** 2026-03-16
**Status:** Draft — Awaiting Approval
**Duration:** 4 weeks (Weeks 17-20)
**Scope:** Fullstack (Backend + API + Frontend)

## Phases

| Phase | Name | Status |
|-------|------|--------|
| 0 | Version Control | completed |
| 1 | Discovery | completed |
| 2 | Research | completed |
| 3 | Planning | completed |
| 4a | API Contract | completed |
| 4b | Implementation | completed |
| 5 | Testing | completed |
| 6 | Documentation | completed |
| 7 | Onboarding | completed |
| 8 | Final Report | completed |

### Success Criteria Checklist

- [x] Branch created: feat/phase-5-dashboard-notifications
- [x] Full-text search across 1000 items completes in <300ms
- [x] Saved filters persist across sessions
- [x] Burn-down chart updates within 30 seconds of a status change
- [x] Velocity chart is correct across 5+ completed sprints
- [x] Email delivered within 1 minute of trigger event
- [x] Failed emails retry 3x with exponential backoff, then dead-letter queue
- [x] Sprint report auto-generated when sprint closes
- [x] Release overdue detected within 24h, email sent to PO + TL
- [x] Data archival job runs off-peak without API performance impact
- [x] Notification preferences respected (disabled type = no delivery)

---

## 1. Overview

Phase 5 delivers four capabilities: full-text search, dashboard analytics, a notification system, and automated background jobs. When complete, leadership has real-time project health data, the system sends emails on key events, and recurring jobs handle velocity tracking, report generation, and data cleanup without manual intervention.

### Success Criteria

1. Full-text search across 1000 items completes in <300ms
2. Saved filters persist across sessions
3. Burn-down chart updates within 30 seconds of a status change
4. Velocity chart is correct across 5+ completed sprints
5. Email delivered within 1 minute of trigger event
6. Failed emails retry 3x with exponential backoff, then dead-letter queue
7. Sprint report auto-generated when sprint closes
8. Release overdue detected within 24h, email sent to PO + TL
9. Data archival job runs off-peak without API performance impact
10. Notification preferences respected (disabled type = no delivery)

---

## 2. Sub-phases

### Dependency Order

```
5.1 Advanced Search         (week 17)     ← Foundation: tsvector trigger, GIN index, search API
5.2 Dashboard & Analytics   (weeks 17-18) ← Depends on: existing burndown, velocity tables
5.3 Notifications           (weeks 18-19) ← Depends on: InAppNotification entity (exists), email service
5.4 Background Automation   (weeks 19-20) ← Depends on: notification infrastructure from 5.3
```

Sub-phases 5.1 and 5.2 can begin in parallel. Sub-phase 5.3 can begin once 5.1 is done. Sub-phase 5.4 depends on 5.3's notification infrastructure.

---

## Sub-phase 5.1: Advanced Search

**Goal:** PostgreSQL full-text search with tsvector + GIN, multi-condition filtering, and saved filters.

### 5.1.1 Database Migration

**New migration:** `AddSearchVectorTriggerAndSavedFilters`

Tables:
```sql
-- New table
CREATE TABLE saved_filters (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID NOT NULL REFERENCES users(id),
  project_id  UUID NOT NULL REFERENCES projects(id),
  name        VARCHAR(100) NOT NULL,
  filter_json JSONB NOT NULL,
  is_default  BOOLEAN NOT NULL DEFAULT FALSE,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (user_id, project_id, name)
);

CREATE INDEX idx_sf_user_project ON saved_filters(user_id, project_id);
```

Trigger on `work_items`:
```sql
-- Automatic tsvector update trigger
CREATE OR REPLACE FUNCTION work_items_search_vector_update() RETURNS trigger AS $$
BEGIN
  NEW.search_vector :=
    setweight(to_tsvector('english', coalesce(NEW.title, '')), 'A') ||
    setweight(to_tsvector('english', coalesce(NEW.description, '')), 'B');
  RETURN NEW;
END
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_work_items_search_vector
  BEFORE INSERT OR UPDATE OF title, description ON work_items
  FOR EACH ROW EXECUTE FUNCTION work_items_search_vector_update();

-- Backfill existing rows
UPDATE work_items SET search_vector =
  setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
  setweight(to_tsvector('english', coalesce(description, '')), 'B');
```

The GIN index `idx_wi_search` already exists on `work_items.search_vector`.

### 5.1.2 Domain

**New entity:**

| File | Description |
|---|---|
| `Domain/Entities/SavedFilter.cs` | `Id`, `UserId`, `ProjectId`, `Name`, `FilterJson` (JsonDocument), `IsDefault`, `CreatedAt`, `UpdatedAt` |

### 5.1.3 Application Layer

**New interface:**

| File | Description |
|---|---|
| `Application/Common/Interfaces/ISavedFilterRepository.cs` | `AddAsync`, `GetByIdAsync`, `ListByUserAndProjectAsync`, `UpdateAsync`, `DeleteAsync` |

**Feature slices:**

| Folder | Files | Description |
|---|---|---|
| `Features/Search/FullTextSearch/` | `FullTextSearchQuery.cs`, `FullTextSearchHandler.cs`, `FullTextSearchValidator.cs` | Multi-condition search: text query (tsvector), status[], priority[], type[], assigneeId, sprintId, releaseId, dateRange. Returns paginated `WorkItemDto` |
| `Features/Search/SaveFilter/` | `SaveFilterCommand.cs`, `SaveFilterHandler.cs`, `SaveFilterValidator.cs` | Saves a named filter config for a user + project |
| `Features/Search/ListSavedFilters/` | `ListSavedFiltersQuery.cs`, `ListSavedFiltersHandler.cs` | Lists all saved filters for user + project |
| `Features/Search/DeleteSavedFilter/` | `DeleteSavedFilterCommand.cs`, `DeleteSavedFilterHandler.cs` | Deletes a saved filter (owner only) |
| `Features/Search/UpdateSavedFilter/` | `UpdateSavedFilterCommand.cs`, `UpdateSavedFilterHandler.cs`, `UpdateSavedFilterValidator.cs` | Updates name or filter config |

**Search query handler logic:**
1. Permission check: user must have project membership
2. Build EF Core query with `WHERE` clauses for each filter
3. For text search: `WHERE search_vector @@ plainto_tsquery('english', :query)`
4. Apply pagination
5. Return `Result<PagedResult<WorkItemDto>>`

### 5.1.4 Infrastructure

| File | Description |
|---|---|
| `Infrastructure/Repositories/SavedFilterRepository.cs` | EF Core implementation of `ISavedFilterRepository` |
| `Infrastructure/Persistence/Configurations/SavedFilterConfiguration.cs` | EF Core entity type config |

**Modify:** `WorkItemRepository.GetBacklogPagedAsync` — replace any existing LIKE-based search with `plainto_tsquery` for the `search` parameter.

### 5.1.5 API

| File | Description |
|---|---|
| `Api/Controllers/SearchController.cs` | `GET /api/v1/search` (full-text + filters), rate limit: `Search` |
| `Api/Controllers/SavedFiltersController.cs` | CRUD on `/api/v1/projects/{projectId}/saved-filters`, rate limit: `Write` for mutations, `General` for reads |

**Endpoints:**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/search?projectId={id}&q={text}&status=ToDo&priority=High&type=Task&assigneeId={id}&sprintId={id}&releaseId={id}&page=1&pageSize=20` | Full-text search with multi-condition filters |
| `POST` | `/api/v1/projects/{projectId}/saved-filters` | Save a filter |
| `GET` | `/api/v1/projects/{projectId}/saved-filters` | List saved filters |
| `PUT` | `/api/v1/projects/{projectId}/saved-filters/{id}` | Update a saved filter |
| `DELETE` | `/api/v1/projects/{projectId}/saved-filters/{id}` | Delete a saved filter |

**Request/response shapes:**

```json
// POST /api/v1/projects/{projectId}/saved-filters
// Request:
{
  "name": "My Sprint Bugs",
  "filterJson": {
    "q": "login",
    "status": ["ToDo", "InProgress"],
    "type": ["Bug"],
    "sprintId": "uuid"
  },
  "isDefault": false
}

// Response: SavedFilterDto
{
  "id": "uuid",
  "name": "My Sprint Bugs",
  "filterJson": { ... },
  "isDefault": false,
  "createdAt": "2026-03-17T10:00:00Z"
}

// GET /api/v1/search response: PagedResult<WorkItemDto>
{
  "items": [ WorkItemDto... ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

### 5.1.6 Frontend

| File | Description |
|---|---|
| `lib/api/search.ts` | Axios client for search and saved-filters endpoints |
| `lib/hooks/use-search.ts` | TanStack Query hooks: `useFullTextSearch`, `useSavedFilters`, `useSaveFilter`, `useDeleteSavedFilter` |
| `lib/stores/search-store.ts` | Zustand store for active search/filter state |
| `app/projects/[projectId]/search/page.tsx` | Search page with multi-condition filter panel |
| `components/search/search-input.tsx` | Debounced full-text search input |
| `components/search/filter-panel.tsx` | Multi-select dropdowns for status, priority, type, assignee, sprint, release |
| `components/search/saved-filter-list.tsx` | Sidebar of saved filters with apply/edit/delete |
| `components/search/search-results.tsx` | Paginated results table |

### 5.1.7 Tests

**TFD order: write failing tests first, then implement.**

| Test file | Scope |
|---|---|
| `Application.Tests/Features/Search/FullTextSearchTests.cs` | Handler: happy path, no results, permission denied, pagination |
| `Application.Tests/Features/Search/SaveFilterTests.cs` | Handler: create, duplicate name conflict, owner validation |
| `Application.Tests/Features/Search/ListSavedFiltersTests.cs` | Handler: list returns user's filters only |
| `Application.Tests/Features/Search/DeleteSavedFilterTests.cs` | Handler: delete own, cannot delete another user's filter |
| `Application.Tests/Features/Search/UpdateSavedFilterTests.cs` | Handler: update name, update filter json |
| `Infrastructure.Tests/Repositories/SavedFilterRepositoryTests.cs` | Integration: CRUD with real PostgreSQL |
| `Infrastructure.Tests/Search/FullTextSearchIntegrationTests.cs` | Integration: tsvector search with GIN index, performance (1000 items <300ms) |
| `Api.Tests/Controllers/SearchControllerTests.cs` | API integration: search endpoint, rate limiting |
| `Api.Tests/Controllers/SavedFiltersControllerTests.cs` | API integration: CRUD saved filters |

---

## Sub-phase 5.2: Dashboard & Analytics

**Goal:** Project dashboard with velocity chart, burn-down chart, cumulative flow diagram, cycle time metrics, team workload heatmap, and release progress.

### 5.2.1 Application Layer

**New DTOs:**

| File | Description |
|---|---|
| `Features/Dashboard/Dtos/VelocityChartDto.cs` | `Sprints[]` with `SprintName`, `PlannedPoints`, `CompletedPoints`, `Velocity`, `Avg3Sprint`, `Avg6Sprint` |
| `Features/Dashboard/Dtos/CumulativeFlowDto.cs` | `DataPoints[]` with `Date`, `ToDo`, `InProgress`, `InReview`, `Done` counts |
| `Features/Dashboard/Dtos/CycleTimeDto.cs` | `ByType[]` with `ItemType`, `AvgDays`, `MedianDays`, `P90Days`, `SampleSize` |
| `Features/Dashboard/Dtos/WorkloadHeatmapDto.cs` | `Members[]` with `UserId`, `Name`, `AssignedCount`, `InProgressCount`, `PointsAssigned` |
| `Features/Dashboard/Dtos/DashboardSummaryDto.cs` | Aggregate: active sprint info, total items, completion %, overdue releases, stale items |
| `Features/Dashboard/Dtos/ReleaseProgressDto.cs` | `DoneCount`, `InProgressCount`, `TodoCount`, `DonePoints`, `TotalPoints`, `CompletionPct` |

**Feature slices:**

| Folder | Files | Description |
|---|---|---|
| `Features/Dashboard/GetVelocityChart/` | Query + Handler | Returns velocity data for last N sprints (default 10). Reads from `team_velocity_history`. |
| `Features/Dashboard/GetCumulativeFlow/` | Query + Handler | Aggregates daily status counts over a date range from `burndown_data_points` + current item statuses. |
| `Features/Dashboard/GetCycleTime/` | Query + Handler | Computes cycle time per item type from `work_item_histories` (time between status transitions). |
| `Features/Dashboard/GetWorkloadHeatmap/` | Query + Handler | Counts assigned items per member in a project. |
| `Features/Dashboard/GetDashboardSummary/` | Query + Handler | Aggregate metrics for the project overview card. |
| `Features/Dashboard/GetReleaseProgress/` | Query + Handler | Returns progress stats for a given release. |

**Handler patterns:**
- All are queries (read-only), return `Result<T>`
- Permission check: user must have project membership (any role)
- Use `AsNoTracking()` projections
- Cycle time query uses `work_item_histories` to calculate elapsed time between `InProgress` and `Done` transitions

### 5.2.2 Infrastructure

**New repository interface and implementation:**

| File | Description |
|---|---|
| `Application/Common/Interfaces/IDashboardRepository.cs` | `GetVelocityDataAsync`, `GetCumulativeFlowDataAsync`, `GetCycleTimeDataAsync`, `GetWorkloadDataAsync`, `GetDashboardSummaryAsync`, `GetReleaseProgressAsync` |
| `Infrastructure/Repositories/DashboardRepository.cs` | Raw SQL + Dapper or EF Core projections for aggregation queries |

### 5.2.3 API

| File | Description |
|---|---|
| `Api/Controllers/DashboardController.cs` | All dashboard endpoints, rate limit: `General` |

**Endpoints:**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/projects/{projectId}/dashboard/summary` | Project overview metrics |
| `GET` | `/api/v1/projects/{projectId}/dashboard/velocity?sprintCount=10` | Velocity chart data |
| `GET` | `/api/v1/projects/{projectId}/dashboard/burndown` | Active sprint burn-down (existing endpoint, re-routed for convenience) |
| `GET` | `/api/v1/projects/{projectId}/dashboard/cumulative-flow?fromDate={}&toDate={}` | Cumulative flow diagram data |
| `GET` | `/api/v1/projects/{projectId}/dashboard/cycle-time?fromDate={}&toDate={}` | Cycle time per item type |
| `GET` | `/api/v1/projects/{projectId}/dashboard/workload` | Team workload heatmap data |
| `GET` | `/api/v1/releases/{releaseId}/progress` | Release progress dashboard |

**Response shapes:**

```json
// GET /api/v1/projects/{id}/dashboard/velocity
{
  "sprints": [
    {
      "sprintId": "uuid",
      "sprintName": "Sprint 14",
      "plannedPoints": 42,
      "completedPoints": 35,
      "velocity": 35,
      "avg3Sprint": 36.0,
      "avg6Sprint": 34.5
    }
  ]
}

// GET /api/v1/projects/{id}/dashboard/cumulative-flow
{
  "dataPoints": [
    {
      "date": "2026-03-17",
      "toDo": 15,
      "inProgress": 8,
      "inReview": 3,
      "done": 12
    }
  ]
}

// GET /api/v1/projects/{id}/dashboard/cycle-time
{
  "byType": [
    {
      "itemType": "UserStory",
      "avgDays": 3.2,
      "medianDays": 2.8,
      "p90Days": 5.1,
      "sampleSize": 24
    }
  ]
}

// GET /api/v1/projects/{id}/dashboard/workload
{
  "members": [
    {
      "userId": "uuid",
      "name": "Hieu N.",
      "assignedCount": 5,
      "inProgressCount": 2,
      "pointsAssigned": 18
    }
  ]
}

// GET /api/v1/projects/{id}/dashboard/summary
{
  "activeSprintId": "uuid",
  "activeSprintName": "Sprint 14",
  "totalItems": 82,
  "openItems": 34,
  "completionPct": 0.585,
  "overdueReleases": 1,
  "staleItems": 3,
  "velocity3SprintAvg": 36.0
}
```

### 5.2.4 SignalR

**Modify:** `IBroadcastService` — add `BroadcastDashboardUpdateAsync(projectId)` method that pushes `dashboard.updated` to `project:{projectId}` group. Called by `WorkItemStatusChangedConsumer` and `SprintCompletedConsumer`.

The frontend listens for `dashboard.updated` and invalidates the TanStack Query cache for dashboard queries (soft refresh, not full reload).

### 5.2.5 Frontend

| File | Description |
|---|---|
| `lib/api/dashboard.ts` | Axios client for all dashboard endpoints |
| `lib/hooks/use-dashboard.ts` | TanStack Query hooks: `useDashboardSummary`, `useVelocityChart`, `useCumulativeFlow`, `useCycleTime`, `useWorkloadHeatmap`, `useReleaseProgress` |
| `app/projects/[projectId]/dashboard/page.tsx` | Dashboard page layout — grid of chart cards |
| `components/dashboard/velocity-chart.tsx` | Bar chart (recharts) — planned vs completed per sprint with trend line |
| `components/dashboard/burndown-chart.tsx` | Line chart — ideal vs actual remaining points |
| `components/dashboard/cumulative-flow-chart.tsx` | Stacked area chart — status counts over time |
| `components/dashboard/cycle-time-chart.tsx` | Box plot or bar chart — avg/median/p90 per item type |
| `components/dashboard/workload-heatmap.tsx` | Heatmap grid — members x assignment intensity |
| `components/dashboard/release-progress-card.tsx` | Donut/ring chart — done/in-progress/todo breakdown |
| `components/dashboard/dashboard-summary-card.tsx` | KPI cards: active sprint, completion %, velocity avg, overdue releases |

**Charting library:** `recharts` (already a common Next.js choice; lightweight, composable).

### 5.2.6 Tests

| Test file | Scope |
|---|---|
| `Application.Tests/Features/Dashboard/GetVelocityChartTests.cs` | Handler: returns correct data across 5+ sprints, empty project |
| `Application.Tests/Features/Dashboard/GetCumulativeFlowTests.cs` | Handler: correct aggregation, date range filtering |
| `Application.Tests/Features/Dashboard/GetCycleTimeTests.cs` | Handler: avg/median/p90 calculation, no data returns empty |
| `Application.Tests/Features/Dashboard/GetWorkloadHeatmapTests.cs` | Handler: correct per-member counts |
| `Application.Tests/Features/Dashboard/GetDashboardSummaryTests.cs` | Handler: aggregate metrics correct |
| `Application.Tests/Features/Dashboard/GetReleaseProgressTests.cs` | Handler: done/todo/in-progress counts |
| `Infrastructure.Tests/Repositories/DashboardRepositoryTests.cs` | Integration: aggregation queries with real PostgreSQL |
| `Api.Tests/Controllers/DashboardControllerTests.cs` | API integration: all 6 endpoints |
| E2E: `e2e/dashboard.spec.ts` | Playwright: dashboard loads, charts render, realtime update after status change |

---

## Sub-phase 5.3: Notifications & Reminders

**Goal:** Email delivery on key events, in-app notification center, per-user preferences, retry with dead-letter queue.

### 5.3.1 Database Migration

**New migration:** `AddNotificationPreferencesAndEmailQueue`

```sql
-- Notification preferences per user
CREATE TABLE notification_preferences (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         UUID NOT NULL REFERENCES users(id),
  notification_type VARCHAR(50) NOT NULL,   -- WorkItemAssigned, DeadlineReminder1d, DeadlineReminder3d,
                                             -- SprintSummary, ReleaseOverdue, MentionNotification
  email_enabled   BOOLEAN NOT NULL DEFAULT TRUE,
  in_app_enabled  BOOLEAN NOT NULL DEFAULT TRUE,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (user_id, notification_type)
);

CREATE INDEX idx_np_user ON notification_preferences(user_id);

-- Email outbox for reliable delivery
CREATE TABLE email_outbox (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  recipient_email VARCHAR(255) NOT NULL,
  recipient_id    UUID REFERENCES users(id),
  template_type   VARCHAR(50) NOT NULL,
  subject         VARCHAR(500) NOT NULL,
  body_json       JSONB NOT NULL,            -- template variables
  status          VARCHAR(20) NOT NULL DEFAULT 'Pending',
                                             -- Pending, Sending, Sent, Failed, DeadLettered
  attempt_count   INTEGER NOT NULL DEFAULT 0,
  max_attempts    INTEGER NOT NULL DEFAULT 3,
  next_retry_at   TIMESTAMPTZ,
  last_error      TEXT,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  sent_at         TIMESTAMPTZ
);

CREATE INDEX idx_eo_status_retry ON email_outbox(status, next_retry_at) WHERE status IN ('Pending', 'Failed');
```

**Modify `in_app_notifications` table** (already exists from Phase 4 entity): add `project_id` column if not present.

### 5.3.2 Domain

**New entities:**

| File | Description |
|---|---|
| `Domain/Entities/NotificationPreference.cs` | `Id`, `UserId`, `NotificationType`, `EmailEnabled`, `InAppEnabled`, `CreatedAt`, `UpdatedAt` |
| `Domain/Entities/EmailOutbox.cs` | `Id`, `RecipientEmail`, `RecipientId`, `TemplateType`, `Subject`, `BodyJson`, `Status`, `AttemptCount`, `MaxAttempts`, `NextRetryAt`, `LastError`, `CreatedAt`, `SentAt` |
| `Domain/Enums/NotificationType.cs` | Enum: `WorkItemAssigned`, `DeadlineReminder1d`, `DeadlineReminder3d`, `SprintSummary`, `ReleaseOverdue`, `MentionNotification` |
| `Domain/Enums/EmailStatus.cs` | Enum: `Pending`, `Sending`, `Sent`, `Failed`, `DeadLettered` |
| `Domain/Events/NotificationCreatedDomainEvent.cs` | `NotificationId`, `RecipientId`, `Type`, `Title` |

### 5.3.3 Application Layer

**New interfaces:**

| File | Description |
|---|---|
| `Application/Common/Interfaces/INotificationPreferenceRepository.cs` | `GetByUserAsync`, `UpsertAsync`, `GetByUserAndTypeAsync` |
| `Application/Common/Interfaces/IEmailOutboxRepository.cs` | `AddAsync`, `GetPendingAsync`, `UpdateAsync`, `GetDeadLetteredAsync` |
| `Application/Common/Interfaces/IEmailSender.cs` | `SendAsync(string to, string subject, string htmlBody, CancellationToken ct)` |
| `Application/Common/Interfaces/INotificationService.cs` | `CreateNotificationAsync(recipientId, type, title, body, referenceId, referenceType, projectId, ct)` — checks user preferences, creates InAppNotification + EmailOutbox entry as needed |

**Feature slices:**

| Folder | Files | Description |
|---|---|---|
| `Features/Notifications/GetNotifications/` | Query + Handler | Paginated list for current user, filter by isRead |
| `Features/Notifications/MarkAsRead/` | Command + Handler | Mark single notification as read |
| `Features/Notifications/MarkAllAsRead/` | Command + Handler | Mark all as read for current user |
| `Features/Notifications/GetUnreadCount/` | Query + Handler | Returns unread count for badge |
| `Features/Notifications/GetPreferences/` | Query + Handler | Returns user's notification preferences |
| `Features/Notifications/UpdatePreferences/` | Command + Handler + Validator | Upsert notification preferences |

### 5.3.4 Infrastructure

| File | Description |
|---|---|
| `Infrastructure/Repositories/NotificationPreferenceRepository.cs` | EF Core implementation |
| `Infrastructure/Repositories/EmailOutboxRepository.cs` | EF Core implementation |
| `Infrastructure/Services/NotificationService.cs` | Implements `INotificationService`: checks preferences, creates InAppNotification (via existing repo), creates EmailOutbox entry, publishes `NotificationCreatedDomainEvent` |
| `Infrastructure/Services/SmtpEmailSender.cs` | Implements `IEmailSender`: sends via SMTP (MailKit). Config: `Email:SmtpHost`, `Email:SmtpPort`, `Email:FromAddress`, `Email:FromName` |
| `Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs` | EF Core config |
| `Infrastructure/Persistence/Configurations/EmailOutboxConfiguration.cs` | EF Core config |

### 5.3.5 Background Services

**New consumers:**

| File | Description |
|---|---|
| `Consumers/WorkItemAssignedNotificationConsumer.cs` | Listens for `WorkItemAssignedDomainEvent`. Calls `INotificationService.CreateNotificationAsync` for the new assignee. |
| `Consumers/NotificationCreatedConsumer.cs` | Listens for `NotificationCreatedDomainEvent`. Broadcasts `notification.created` via SignalR to `user:{recipientId}` group. |

**New scheduled job:**

| File | Description |
|---|---|
| `Scheduled/Jobs/EmailOutboxProcessorJob.cs` | **Cron:** `*/30 * * * * ?` (every 30 seconds). Queries `email_outbox WHERE status IN ('Pending', 'Failed') AND next_retry_at <= NOW()`. For each: attempt send via `IEmailSender`. On success: set `Sent`. On failure: increment `attempt_count`, set `next_retry_at` with exponential backoff (`30s, 5m, 30m`). If `attempt_count >= max_attempts`: set `DeadLettered`, log alert. |
| `Scheduled/Jobs/DeadlineReminderJob.cs` | **Cron:** `0 8 * * ?` (08:00 AM daily). Queries work items with: release date 1 day or 3 days away AND status not Done/Rejected. Creates reminder notification for assignee. Respects `DeadlineReminder1d` / `DeadlineReminder3d` preferences. |

### 5.3.6 API

| File | Description |
|---|---|
| `Api/Controllers/NotificationsController.cs` | Notification center endpoints, rate limit: `General` for reads, `Write` for mutations |

**Endpoints:**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/notifications?isRead={bool}&page=1&pageSize=20` | List notifications for current user |
| `GET` | `/api/v1/notifications/unread-count` | Unread count for badge |
| `POST` | `/api/v1/notifications/{id}/read` | Mark single as read |
| `POST` | `/api/v1/notifications/read-all` | Mark all as read |
| `GET` | `/api/v1/notifications/preferences` | Get current user's preferences |
| `PUT` | `/api/v1/notifications/preferences` | Update preferences |

**Request/response shapes:**

```json
// GET /api/v1/notifications
{
  "items": [
    {
      "id": "uuid",
      "type": "WorkItemAssigned",
      "title": "You were assigned to TF-142",
      "body": "Task: Fix login timeout",
      "referenceId": "uuid",
      "referenceType": "WorkItem",
      "isRead": false,
      "createdAt": "2026-03-17T10:00:00Z"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 20
}

// GET /api/v1/notifications/unread-count
{ "count": 5 }

// GET /api/v1/notifications/preferences
{
  "preferences": [
    {
      "notificationType": "WorkItemAssigned",
      "emailEnabled": true,
      "inAppEnabled": true
    },
    {
      "notificationType": "DeadlineReminder1d",
      "emailEnabled": true,
      "inAppEnabled": true
    }
  ]
}

// PUT /api/v1/notifications/preferences
{
  "preferences": [
    {
      "notificationType": "WorkItemAssigned",
      "emailEnabled": false,
      "inAppEnabled": true
    }
  ]
}
```

### 5.3.7 SignalR

**Existing:** `user:{userId}` group already defined. The `NotificationCreatedConsumer` broadcasts `notification.created` with `{ id, type, title }` to trigger badge update and toast on the frontend.

### 5.3.8 Frontend

| File | Description |
|---|---|
| `lib/api/notifications.ts` | Axios client for notification endpoints |
| `lib/hooks/use-notifications.ts` | TanStack Query hooks: `useNotifications`, `useUnreadCount`, `useMarkAsRead`, `useMarkAllAsRead`, `useNotificationPreferences`, `useUpdatePreferences` |
| `lib/stores/notification-store.ts` | Zustand store for unread badge count (updated by SignalR) |
| `components/notifications/notification-bell.tsx` | Topbar bell icon with unread badge, dropdown panel |
| `components/notifications/notification-list.tsx` | Full notification list with pagination, read/unread filter |
| `components/notifications/notification-item.tsx` | Single notification card with link to referenced entity |
| `components/notifications/notification-preferences.tsx` | Settings panel: toggle email/in-app per notification type |
| `app/projects/[projectId]/notifications/page.tsx` | Full notification center page (alternative to dropdown) |

### 5.3.9 Tests

| Test file | Scope |
|---|---|
| `Application.Tests/Features/Notifications/GetNotificationsTests.cs` | Handler: paginated list, filter by isRead, returns only current user's |
| `Application.Tests/Features/Notifications/MarkAsReadTests.cs` | Handler: marks own notification, rejects other user's |
| `Application.Tests/Features/Notifications/MarkAllAsReadTests.cs` | Handler: marks all for current user |
| `Application.Tests/Features/Notifications/GetUnreadCountTests.cs` | Handler: returns correct count |
| `Application.Tests/Features/Notifications/GetPreferencesTests.cs` | Handler: returns user preferences, defaults when none set |
| `Application.Tests/Features/Notifications/UpdatePreferencesTests.cs` | Handler: upsert preferences |
| `Infrastructure.Tests/Services/NotificationServiceTests.cs` | Integration: preference check, creates InAppNotification + EmailOutbox |
| `BackgroundServices.Tests/Jobs/EmailOutboxProcessorJobTests.cs` | Unit: processes pending, retries failed with backoff, dead-letters after 3 failures |
| `BackgroundServices.Tests/Jobs/DeadlineReminderJobTests.cs` | Unit: detects items due in 1d/3d, respects preferences |
| `BackgroundServices.Tests/Consumers/WorkItemAssignedNotificationConsumerTests.cs` | Unit: creates notification on assignment |
| `Api.Tests/Controllers/NotificationsControllerTests.cs` | API integration: all endpoints |
| E2E: `e2e/notifications.spec.ts` | Playwright: notification bell updates on assignment, mark as read, preferences save |

---

## Sub-phase 5.4: Background Automation

**Goal:** Four scheduled jobs: VelocityAggregatorJob, SprintReportGeneratorJob (enhanced), DataArchivalJob, TeamHealthSummaryJob.

### 5.4.1 Database Migration

**New migration:** `AddSprintReportsAndTeamHealthTables`

```sql
CREATE TABLE sprint_reports (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sprint_id         UUID NOT NULL REFERENCES sprints(id),
  project_id        UUID NOT NULL REFERENCES projects(id),
  report_data       JSONB NOT NULL,           -- Full report payload
  generated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  generated_by      VARCHAR(20) NOT NULL DEFAULT 'System', -- System, Manual
  UNIQUE (sprint_id)
);

CREATE TABLE team_health_summaries (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  project_id      UUID NOT NULL REFERENCES projects(id),
  period_start    DATE NOT NULL,
  period_end      DATE NOT NULL,
  summary_data    JSONB NOT NULL,           -- Health metrics payload
  generated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (project_id, period_start)
);

CREATE INDEX idx_ths_project ON team_health_summaries(project_id, period_start DESC);
```

### 5.4.2 Domain

**New entities:**

| File | Description |
|---|---|
| `Domain/Entities/SprintReport.cs` | `Id`, `SprintId`, `ProjectId`, `ReportData` (JsonDocument), `GeneratedAt`, `GeneratedBy` |
| `Domain/Entities/TeamHealthSummary.cs` | `Id`, `ProjectId`, `PeriodStart`, `PeriodEnd`, `SummaryData` (JsonDocument), `GeneratedAt` |

### 5.4.3 Application Layer

**New interfaces:**

| File | Description |
|---|---|
| `Application/Common/Interfaces/ISprintReportRepository.cs` | `AddAsync`, `GetBySprintIdAsync`, `ListByProjectAsync` |
| `Application/Common/Interfaces/ITeamHealthSummaryRepository.cs` | `AddAsync`, `GetLatestByProjectAsync`, `ListByProjectAsync` |

**Feature slices:**

| Folder | Files | Description |
|---|---|---|
| `Features/Reports/GetSprintReport/` | Query + Handler | Returns the generated report for a sprint |
| `Features/Reports/ListSprintReports/` | Query + Handler | Paginated list of reports for a project |
| `Features/Reports/GetTeamHealthSummary/` | Query + Handler | Returns latest health summary for a project |
| `Features/Reports/ListTeamHealthSummaries/` | Query + Handler | Paginated list of health summaries |

### 5.4.4 Infrastructure

| File | Description |
|---|---|
| `Infrastructure/Repositories/SprintReportRepository.cs` | EF Core implementation |
| `Infrastructure/Repositories/TeamHealthSummaryRepository.cs` | EF Core implementation |
| `Infrastructure/Persistence/Configurations/SprintReportConfiguration.cs` | EF Core config |
| `Infrastructure/Persistence/Configurations/TeamHealthSummaryConfiguration.cs` | EF Core config |

### 5.4.5 Background Services — Jobs

| File | Cron | Description |
|---|---|---|
| `Scheduled/Jobs/VelocityAggregatorJob.cs` | `0 7 * * 1` (Mon 07:00) | For each project with completed sprints: recalculate `velocity_3sprint_avg`, `velocity_6sprint_avg`, linear regression trend, confidence intervals. Detect anomaly (velocity < lower bound) and notify TL. Update `team_velocity_history`. Set `recommended_points = avg3 * 0.85`. |
| `Scheduled/Jobs/SprintReportGeneratorJob.cs` | Event-triggered (SprintCompletedConsumer enqueues) | Gather: SprintSnapshot, DomainEvents, WorkItems, TeamVelocityHistory, BurndownDataPoints. Compute: predictability score, quality score, blocker impact. Store in `sprint_reports`. Send email to PO + TL + Team Manager. Broadcast `sprint.report_ready`. |
| `Scheduled/Jobs/DataArchivalJob.cs` | `0 3 1 * ?` (1st of month 03:00) | Phase 1: Identify candidates (DomainEvents >36mo, soft-deleted WorkItems >30d). Phase 2: Export to gzipped JSON lines. Phase 3: Hard delete soft-deleted work items (WorkItemHistories preserved). Idempotent: skip already-archived ranges. **Note:** S3 upload is out of scope for initial implementation; export to local file system with clear interface for future S3 adapter. |
| `Scheduled/Jobs/TeamHealthSummaryJob.cs` | `0 7 * * 1` (Mon 07:00, after velocity) | Aggregates weekly metrics per project: velocity trend, bug rate, rework rate, stale item count, avg cycle time, sprint predictability. Stores in `team_health_summaries`. Sends summary email to TL + Team Manager. |

### 5.4.6 API

| File | Description |
|---|---|
| `Api/Controllers/ReportsController.cs` | Report retrieval endpoints, rate limit: `General` |

**Endpoints:**

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/sprints/{sprintId}/report` | Get sprint report |
| `GET` | `/api/v1/projects/{projectId}/reports/sprints?page=1&pageSize=10` | List sprint reports |
| `GET` | `/api/v1/projects/{projectId}/reports/team-health/latest` | Latest team health summary |
| `GET` | `/api/v1/projects/{projectId}/reports/team-health?page=1&pageSize=10` | List team health summaries |

### 5.4.7 Frontend

| File | Description |
|---|---|
| `lib/api/reports.ts` | Axios client for report endpoints |
| `lib/hooks/use-reports.ts` | TanStack Query hooks: `useSprintReport`, `useSprintReports`, `useTeamHealthSummary`, `useTeamHealthSummaries` |
| `app/projects/[projectId]/reports/page.tsx` | Reports list page |
| `components/reports/sprint-report-card.tsx` | Sprint report display: metrics, charts, quality score |
| `components/reports/team-health-card.tsx` | Team health summary card: trends, alerts |

### 5.4.8 Tests

| Test file | Scope |
|---|---|
| `BackgroundServices.Tests/Jobs/VelocityAggregatorJobTests.cs` | Calculates averages, detects anomaly, updates history |
| `BackgroundServices.Tests/Jobs/SprintReportGeneratorJobTests.cs` | Gathers data, computes metrics, stores report |
| `BackgroundServices.Tests/Jobs/DataArchivalJobTests.cs` | Identifies candidates, exports, hard deletes soft-deleted items, idempotent |
| `BackgroundServices.Tests/Jobs/TeamHealthSummaryJobTests.cs` | Aggregates weekly metrics, stores summary |
| `Application.Tests/Features/Reports/GetSprintReportTests.cs` | Handler: returns report, 404 if not generated |
| `Application.Tests/Features/Reports/ListSprintReportsTests.cs` | Handler: paginated list |
| `Application.Tests/Features/Reports/GetTeamHealthSummaryTests.cs` | Handler: returns latest summary |
| `Api.Tests/Controllers/ReportsControllerTests.cs` | API integration: all endpoints |
| E2E: `e2e/reports.spec.ts` | Playwright: sprint report page loads after sprint close |

---

## 3. Dependencies

```
5.1 Advanced Search
  ├── search_vector trigger (migration)
  ├── saved_filters table (migration)
  └── No dependency on other sub-phases

5.2 Dashboard & Analytics
  ├── Existing: burndown_data_points, team_velocity_history, sprint_snapshots
  ├── Existing: work_item_histories (for cycle time)
  └── No dependency on 5.1 or 5.3

5.3 Notifications & Reminders
  ├── Existing: InAppNotification entity, IInAppNotificationRepository
  ├── New: notification_preferences table
  ├── New: email_outbox table
  ├── New: IEmailSender + SmtpEmailSender
  └── No dependency on 5.1 or 5.2

5.4 Background Automation
  ├── Depends on 5.3: INotificationService (for email delivery from jobs)
  ├── Depends on 5.3: EmailOutboxProcessorJob (for reliable email sending)
  ├── New: sprint_reports table
  └── New: team_health_summaries table
```

---

## 4. Database Migrations Summary

| Migration | Sub-phase | Tables/Changes |
|---|---|---|
| `AddSearchVectorTriggerAndSavedFilters` | 5.1 | `saved_filters` table, tsvector trigger on `work_items`, backfill search_vector |
| `AddNotificationPreferencesAndEmailQueue` | 5.3 | `notification_preferences`, `email_outbox` tables, add `project_id` to `in_app_notifications` if missing |
| `AddSprintReportsAndTeamHealthTables` | 5.4 | `sprint_reports`, `team_health_summaries` tables |

---

## 5. File Inventory

### New Files — Backend

**Domain (6 files)**
- `src/core/TeamFlow.Domain/Entities/SavedFilter.cs`
- `src/core/TeamFlow.Domain/Entities/NotificationPreference.cs`
- `src/core/TeamFlow.Domain/Entities/EmailOutbox.cs`
- `src/core/TeamFlow.Domain/Entities/SprintReport.cs`
- `src/core/TeamFlow.Domain/Entities/TeamHealthSummary.cs`
- `src/core/TeamFlow.Domain/Enums/NotificationType.cs`
- `src/core/TeamFlow.Domain/Enums/EmailStatus.cs`
- `src/core/TeamFlow.Domain/Events/NotificationCreatedDomainEvent.cs`

**Application — Interfaces (6 files)**
- `src/core/TeamFlow.Application/Common/Interfaces/ISavedFilterRepository.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/INotificationPreferenceRepository.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/IEmailOutboxRepository.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/IEmailSender.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/INotificationService.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/IDashboardRepository.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/ISprintReportRepository.cs`
- `src/core/TeamFlow.Application/Common/Interfaces/ITeamHealthSummaryRepository.cs`

**Application — Feature Slices (22 slice folders, ~60 files)**

Search (5 slices, 13 files):
- `src/core/TeamFlow.Application/Features/Search/FullTextSearch/` (3 files)
- `src/core/TeamFlow.Application/Features/Search/SaveFilter/` (3 files)
- `src/core/TeamFlow.Application/Features/Search/ListSavedFilters/` (2 files)
- `src/core/TeamFlow.Application/Features/Search/DeleteSavedFilter/` (2 files)
- `src/core/TeamFlow.Application/Features/Search/UpdateSavedFilter/` (3 files)

Dashboard (6 slices, 12 files):
- `src/core/TeamFlow.Application/Features/Dashboard/GetVelocityChart/` (2 files)
- `src/core/TeamFlow.Application/Features/Dashboard/GetCumulativeFlow/` (2 files)
- `src/core/TeamFlow.Application/Features/Dashboard/GetCycleTime/` (2 files)
- `src/core/TeamFlow.Application/Features/Dashboard/GetWorkloadHeatmap/` (2 files)
- `src/core/TeamFlow.Application/Features/Dashboard/GetDashboardSummary/` (2 files)
- `src/core/TeamFlow.Application/Features/Dashboard/GetReleaseProgress/` (2 files)
- `src/core/TeamFlow.Application/Features/Dashboard/Dtos/` (6 DTO files)

Notifications (6 slices, 14 files):
- `src/core/TeamFlow.Application/Features/Notifications/GetNotifications/` (2 files)
- `src/core/TeamFlow.Application/Features/Notifications/MarkAsRead/` (2 files)
- `src/core/TeamFlow.Application/Features/Notifications/MarkAllAsRead/` (2 files)
- `src/core/TeamFlow.Application/Features/Notifications/GetUnreadCount/` (2 files)
- `src/core/TeamFlow.Application/Features/Notifications/GetPreferences/` (2 files)
- `src/core/TeamFlow.Application/Features/Notifications/UpdatePreferences/` (3 files)

Reports (4 slices, 8 files):
- `src/core/TeamFlow.Application/Features/Reports/GetSprintReport/` (2 files)
- `src/core/TeamFlow.Application/Features/Reports/ListSprintReports/` (2 files)
- `src/core/TeamFlow.Application/Features/Reports/GetTeamHealthSummary/` (2 files)
- `src/core/TeamFlow.Application/Features/Reports/ListTeamHealthSummaries/` (2 files)

**Infrastructure (12 files)**
- `src/core/TeamFlow.Infrastructure/Repositories/SavedFilterRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/DashboardRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/NotificationPreferenceRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/EmailOutboxRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/SprintReportRepository.cs`
- `src/core/TeamFlow.Infrastructure/Repositories/TeamHealthSummaryRepository.cs`
- `src/core/TeamFlow.Infrastructure/Services/NotificationService.cs`
- `src/core/TeamFlow.Infrastructure/Services/SmtpEmailSender.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/SavedFilterConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/EmailOutboxConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/SprintReportConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/TeamHealthSummaryConfiguration.cs`

**API (4 controllers)**
- `src/apps/TeamFlow.Api/Controllers/SearchController.cs`
- `src/apps/TeamFlow.Api/Controllers/SavedFiltersController.cs`
- `src/apps/TeamFlow.Api/Controllers/DashboardController.cs`
- `src/apps/TeamFlow.Api/Controllers/NotificationsController.cs`
- `src/apps/TeamFlow.Api/Controllers/ReportsController.cs`

**Background Services (6 files)**
- `src/apps/TeamFlow.BackgroundServices/Consumers/WorkItemAssignedNotificationConsumer.cs`
- `src/apps/TeamFlow.BackgroundServices/Consumers/NotificationCreatedConsumer.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/EmailOutboxProcessorJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/DeadlineReminderJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/VelocityAggregatorJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/SprintReportGeneratorJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/DataArchivalJob.cs`
- `src/apps/TeamFlow.BackgroundServices/Scheduled/Jobs/TeamHealthSummaryJob.cs`

### New Files — Frontend

**API clients (4 files)**
- `src/apps/teamflow-web/lib/api/search.ts`
- `src/apps/teamflow-web/lib/api/dashboard.ts`
- `src/apps/teamflow-web/lib/api/notifications.ts`
- `src/apps/teamflow-web/lib/api/reports.ts`

**Hooks (4 files)**
- `src/apps/teamflow-web/lib/hooks/use-search.ts`
- `src/apps/teamflow-web/lib/hooks/use-dashboard.ts`
- `src/apps/teamflow-web/lib/hooks/use-notifications.ts`
- `src/apps/teamflow-web/lib/hooks/use-reports.ts`

**Stores (2 files)**
- `src/apps/teamflow-web/lib/stores/search-store.ts`
- `src/apps/teamflow-web/lib/stores/notification-store.ts`

**Pages (4 pages)**
- `src/apps/teamflow-web/app/projects/[projectId]/search/page.tsx`
- `src/apps/teamflow-web/app/projects/[projectId]/dashboard/page.tsx`
- `src/apps/teamflow-web/app/projects/[projectId]/notifications/page.tsx`
- `src/apps/teamflow-web/app/projects/[projectId]/reports/page.tsx`

**Components (17 files)**
- `src/apps/teamflow-web/components/search/search-input.tsx`
- `src/apps/teamflow-web/components/search/filter-panel.tsx`
- `src/apps/teamflow-web/components/search/saved-filter-list.tsx`
- `src/apps/teamflow-web/components/search/search-results.tsx`
- `src/apps/teamflow-web/components/dashboard/velocity-chart.tsx`
- `src/apps/teamflow-web/components/dashboard/burndown-chart.tsx`
- `src/apps/teamflow-web/components/dashboard/cumulative-flow-chart.tsx`
- `src/apps/teamflow-web/components/dashboard/cycle-time-chart.tsx`
- `src/apps/teamflow-web/components/dashboard/workload-heatmap.tsx`
- `src/apps/teamflow-web/components/dashboard/release-progress-card.tsx`
- `src/apps/teamflow-web/components/dashboard/dashboard-summary-card.tsx`
- `src/apps/teamflow-web/components/notifications/notification-bell.tsx`
- `src/apps/teamflow-web/components/notifications/notification-list.tsx`
- `src/apps/teamflow-web/components/notifications/notification-item.tsx`
- `src/apps/teamflow-web/components/notifications/notification-preferences.tsx`
- `src/apps/teamflow-web/components/reports/sprint-report-card.tsx`
- `src/apps/teamflow-web/components/reports/team-health-card.tsx`

### New Files — Tests

**Application Tests (20+ files)**
- `tests/TeamFlow.Application.Tests/Features/Search/FullTextSearchTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Search/SaveFilterTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Search/ListSavedFiltersTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Search/DeleteSavedFilterTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Search/UpdateSavedFilterTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Dashboard/GetVelocityChartTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Dashboard/GetCumulativeFlowTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Dashboard/GetCycleTimeTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Dashboard/GetWorkloadHeatmapTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Dashboard/GetDashboardSummaryTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Dashboard/GetReleaseProgressTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Notifications/GetNotificationsTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Notifications/MarkAsReadTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Notifications/MarkAllAsReadTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Notifications/GetUnreadCountTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Notifications/GetPreferencesTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Notifications/UpdatePreferencesTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Reports/GetSprintReportTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Reports/ListSprintReportsTests.cs`
- `tests/TeamFlow.Application.Tests/Features/Reports/GetTeamHealthSummaryTests.cs`

**Infrastructure Tests (3 files)**
- `tests/TeamFlow.Infrastructure.Tests/Repositories/SavedFilterRepositoryTests.cs`
- `tests/TeamFlow.Infrastructure.Tests/Search/FullTextSearchIntegrationTests.cs`
- `tests/TeamFlow.Infrastructure.Tests/Services/NotificationServiceTests.cs`
- `tests/TeamFlow.Infrastructure.Tests/Repositories/DashboardRepositoryTests.cs`

**Background Services Tests (6 files)**
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/EmailOutboxProcessorJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/DeadlineReminderJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/VelocityAggregatorJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/SprintReportGeneratorJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/DataArchivalJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Jobs/TeamHealthSummaryJobTests.cs`
- `tests/TeamFlow.BackgroundServices.Tests/Consumers/WorkItemAssignedNotificationConsumerTests.cs`

**API Tests (5 files)**
- `tests/TeamFlow.Api.Tests/Controllers/SearchControllerTests.cs`
- `tests/TeamFlow.Api.Tests/Controllers/SavedFiltersControllerTests.cs`
- `tests/TeamFlow.Api.Tests/Controllers/DashboardControllerTests.cs`
- `tests/TeamFlow.Api.Tests/Controllers/NotificationsControllerTests.cs`
- `tests/TeamFlow.Api.Tests/Controllers/ReportsControllerTests.cs`

**E2E Tests (4 files)**
- `e2e/dashboard.spec.ts`
- `e2e/search.spec.ts`
- `e2e/notifications.spec.ts`
- `e2e/reports.spec.ts`

### Modified Files

| File | Change |
|---|---|
| `Infrastructure/Persistence/TeamFlowDbContext.cs` | Add `DbSet` for `SavedFilter`, `NotificationPreference`, `EmailOutbox`, `SprintReport`, `TeamHealthSummary` |
| `Infrastructure/DependencyInjection.cs` | Register new repositories and services |
| `BackgroundServices/DependencyInjection.cs` (or `Program.cs`) | Register new Quartz jobs and MassTransit consumers |
| `Infrastructure/Repositories/WorkItemRepository.cs` | Replace LIKE search with `plainto_tsquery` in `GetBacklogPagedAsync` |
| `BackgroundServices/Consumers/SprintCompletedConsumer.cs` | Enqueue `SprintReportGeneratorJob` after sprint completion |
| `Api/Hubs/TeamFlowHub.cs` | No change needed (groups already support `user:{userId}`) |
| `Infrastructure/Services/BroadcastService.cs` | Add `BroadcastDashboardUpdateAsync` and `BroadcastNotificationCreatedAsync` methods |
| `Application/Common/Interfaces/IBroadcastService.cs` | Add new broadcast method signatures |
| `appsettings.json` / `appsettings.Development.json` | Add `Email:SmtpHost`, `Email:SmtpPort`, `Email:FromAddress`, `Email:FromName` config section |
| `docker-compose.yml` | Add MailHog service for local email testing |

---

## 6. Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Full-text search performance degrades beyond 1000 items | High | Low | GIN index already exists; tsvector trigger keeps index warm; benchmark with 5000 items during integration tests |
| Email delivery latency exceeds 1-minute SLA | Medium | Medium | Outbox processor runs every 30 seconds; test with MailHog locally; add job metrics alerting |
| DataArchivalJob deletes data incorrectly | High | Low | Hard delete only for items soft-deleted >30 days; WorkItemHistories never deleted; add dry-run mode; human reviews implementation |
| Dashboard queries slow on large datasets | Medium | Medium | Use `AsNoTracking` + projections; pre-aggregated data in `team_velocity_history` and `burndown_data_points`; add indexes if needed |
| Exponential backoff logic is incorrect | Medium | Low | Dedicated unit tests with mocked clock for each retry interval |
| SprintReportGeneratorJob timeout (>2 min) | Medium | Low | Parallel queries for data gathering; monitor via `job_execution_metrics` |
| tsvector trigger adds write latency | Low | Low | Trigger is lightweight (two `to_tsvector` calls); measure in integration test |
| Notification spam if preferences not checked | Medium | Medium | `INotificationService` always checks preferences before creating; test with disabled preference returns no notification |

### Items Requiring Human Review

Per CLAUDE.md rules:
- **SmtpEmailSender** — human reviews email sending implementation (external service integration)
- **DataArchivalJob** — human reviews all hard-delete logic (irreversible operation)
- **Migration with tsvector trigger** — human verifies SQL correctness before applying

---

## 7. Implementation Order Within Each Sub-phase

Each sub-phase follows this sequence:

1. **Migration** — create/apply database migration
2. **Domain entities** — new sealed entity classes
3. **Application interfaces** — repository and service interfaces
4. **Tests (TFD)** — write failing handler tests
5. **Application handlers** — implement to make tests pass
6. **Infrastructure** — repository and service implementations
7. **Tests (TFD)** — write failing integration tests
8. **Infrastructure tests** — verify with real PostgreSQL
9. **API controller** — thin controller calling `Sender.Send()`
10. **API tests** — integration tests for endpoints
11. **Frontend** — API client, hooks, pages, components
12. **E2E tests** — Playwright tests for the full flow

All classes sealed by default. All handlers return `Result<T>`. All command handlers check permissions via `IPermissionChecker`. All errors return `ProblemDetails`.

---

## Approval

This plan covers 4 sub-phases across 4 weeks. Total new files: ~130 (backend ~85, frontend ~31, tests ~40). Total modified files: ~10.

Approve this plan to proceed to implementation.
