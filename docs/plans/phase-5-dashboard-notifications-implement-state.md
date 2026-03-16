# Implementation State — Phase 5: Dashboard, Notifications & Search

## Topic
Phase 5 fullstack implementation: Dashboard & Analytics, Notifications & Reminders, Background Automation, Advanced Search

## Discovery Context

- **Branch:** `feat/phase-5-dashboard-notifications` (create from `main`)
- **Requirements:** Follow plan at `docs/plans/phase-5-dashboard-notifications/plan.md` — no additional requirements
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)
- **Feature Scope:** Fullstack (Frontend + Backend + API)
- **Task Type:** feature

## Phase-Specific Context

- **Plan directory:** `docs/plans/phase-5-dashboard-notifications`
- **Plan source:** `docs/plans/phase-5-dashboard-notifications/plan.md`
- **User modifications:** None

### Plan Summary

4 sub-phases over 4 weeks, ~130 new files:

1. **Sub-phase 5.1 (Advanced Search):** tsvector + GIN index, multi-condition filtering, saved filters. New `saved_filters` table, 5 feature slices, `SearchController` + `SavedFiltersController`.

2. **Sub-phase 5.2 (Dashboard & Analytics):** 6 chart/metric endpoints (velocity, burn-down, cumulative flow, cycle time, workload heatmap, release progress). `DashboardController` + recharts frontend. SignalR pushes `dashboard.updated`.

3. **Sub-phase 5.3 (Notifications & Reminders):** Email outbox with exponential backoff (3 retries then dead-letter), in-app notification center, per-user preferences, `DeadlineReminderJob`. `NotificationsController`.

4. **Sub-phase 5.4 (Background Automation):** 4 Quartz jobs (VelocityAggregator, SprintReportGenerator, DataArchival, TeamHealthSummary). New `sprint_reports` and `team_health_summaries` tables. `ReportsController`.

### Key Constraints
- TFD: Write failing tests first, then implement
- All classes sealed by default
- CQRS with MediatR, Result<T> pattern
- Permission checks in every command handler
- ProblemDetails for all errors
- Follow existing vertical slice folder structure
- Testcontainers with real PostgreSQL for integration tests
- Frontend: TanStack Query + Zustand + recharts
- All background jobs must be idempotent
- DataArchivalJob: local filesystem export (S3 deferred)
