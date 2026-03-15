# API Contract -- Phase 3.3 Background Jobs

**Version:** 1.0.0
**Date:** 2026-03-15
**Scope:** Background jobs and MassTransit consumers (no new HTTP endpoints)

---

## Overview

Phase 3.3 adds four Quartz.NET scheduled jobs and two MassTransit consumers. No new REST endpoints are introduced. All interactions are event-driven via RabbitMQ (MassTransit) and SignalR broadcasts.

---

## Scheduled Jobs

### BurndownSnapshotJob

| Property | Value |
|----------|-------|
| Schedule | `59 23 * * ?` (11:59 PM daily) |
| Misfire | FireNow |
| Concurrency | DisallowConcurrent |
| Priority | High |

**Produces:**
- `BurndownDataPoint` record per active sprint
- SignalR broadcast: `burndown.updated` to sprint group

**Side Effects:**
- Logs "At Risk" warning when remaining > ideal * 1.2

---

### ReleaseOverdueDetectorJob

| Property | Value |
|----------|-------|
| Schedule | `5 0 * * ?` (00:05 AM daily) |
| Misfire | FireNow |
| Concurrency | DisallowConcurrent |
| Priority | High |

**Produces:**
- Updates Release.Status from Unreleased to Overdue
- Publishes `ReleaseOverdueDetectedDomainEvent` via MediatR
- SignalR broadcast: `release.overdue_detected` to project group

---

### StaleItemDetectorJob

| Property | Value |
|----------|-------|
| Schedule | `0 8 * * ?` (08:00 AM daily) |
| Misfire | DoNothing |
| Concurrency | DisallowConcurrent |
| Priority | Medium |

**Produces:**
- Sets `ai_metadata.stale_flag = true` on stale work items
- Publishes `WorkItemStaleFlaggedDomainEvent` via MediatR
- Severity: Critical (in active sprint), High (in release), Medium (assigned), Low (unassigned)

**Stale criteria:**
- Status NOT IN (Done, Rejected)
- updated_at < NOW() - 14 days
- deleted_at IS NULL
- Project status != "Archived"

---

### EventPartitionCreatorJob

| Property | Value |
|----------|-------|
| Schedule | `0 3 25 * ?` (03:00 AM, 25th monthly) |
| Misfire | FireNow |
| Concurrency | DisallowConcurrent |
| Priority | Critical |

**Produces:**
- PostgreSQL partition: `domain_events_YYYY_MM`
- Idempotent (CREATE TABLE IF NOT EXISTS)

---

## MassTransit Consumers

### SprintStartedConsumer

**Consumes:** `SprintStartedDomainEvent`

**Produces:**
- `SprintSnapshot` (type: "OnStart")
- `BurndownDataPoint` (initial, day 0)
- SignalR broadcast: `sprint.started` to project group

### SprintCompletedConsumer

**Consumes:** `SprintCompletedDomainEvent`

**Produces:**
- `SprintSnapshot` (type: "OnClose", is_final: true)
- `TeamVelocityHistory` record
- SignalR broadcast: `sprint.completed` to project group

---

## Domain Events (already defined)

| Event | File |
|-------|------|
| `ReleaseOverdueDetectedDomainEvent` | `Domain/Events/ReleaseDomainEvents.cs` |
| `WorkItemStaleFlaggedDomainEvent` | `Domain/Events/WorkItemDomainEvents.cs` |
| `SprintStartedDomainEvent` | `Domain/Events/SprintDomainEvents.cs` |
| `SprintCompletedDomainEvent` | `Domain/Events/SprintDomainEvents.cs` |

---

## SignalR Broadcast Events

| Event Name | Group | Payload |
|------------|-------|---------|
| `burndown.updated` | Sprint | `{ sprintId, recordedDate, remainingPoints, completedPoints }` |
| `release.overdue_detected` | Project | `{ releaseId, projectId, releaseName, releaseDate }` |
| `sprint.started` | Project | `SprintStartedDomainEvent` |
| `sprint.completed` | Project | `SprintCompletedDomainEvent` |

---

## Shared Types

No new DTOs. Jobs operate on domain entities directly.

---

## TBD / Pending

None -- all contracts defined in Phase 3.0 plan.
