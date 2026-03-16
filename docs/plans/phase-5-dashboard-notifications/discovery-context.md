# Discovery Context — Phase 5: Dashboard, Notifications & Search

**Created:** 2026-03-16
**Status:** Planning

## Requirements

Phase 5 goal: Leadership has data. System automates reporting and reminders.

### Scope: Fullstack (Frontend + Backend + API)

All 4 Phase 5 areas included:

1. **Dashboard & Analytics** — Velocity chart, burn-down chart, cumulative flow diagram, cycle time per item type, team workload heatmap, release progress dashboard
2. **Notifications & Reminders** — Email on assignment, deadline reminders (1d/3d), sprint summary email, release overdue email, in-app notification center, per-user preferences, failed delivery with exponential backoff x3 → dead-letter queue
3. **Background Automation** — SprintReportGeneratorJob (on sprint close), VelocityAggregatorJob (Monday 07:00), DataArchivalJob (1st of month 03:00), TeamHealthSummaryJob (weekly)
4. **Advanced Search** — Full-text search (PostgreSQL tsvector + GIN), multi-condition filters, saved filters per user

### Testing Requirements

- Unit tests (xUnit + FluentAssertions + NSubstitute)
- Integration tests (Testcontainers + real PostgreSQL)
- E2E tests (Playwright)
- TFD workflow: write failing tests first, then implement

## Success Criteria (from phases.md)

1. Burn-down updates within 30 seconds of status change
2. Email delivered within 1 minute of trigger
3. Failed email retries x3 with backoff → dead-letter queue
4. Sprint report auto-generated at sprint close
5. Release overdue detected within 24h → email sent
6. Cleanup job runs without API performance impact (off-peak)
7. Velocity chart correct across ≥5 completed sprints
8. Notification preference respected
9. Full-text search 1000 items <300ms
10. Saved filter persists across sessions
