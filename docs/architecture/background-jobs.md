# 07 — Background Jobs

## Job Types

| Type | Trigger | Examples |
|---|---|---|
| **Event-driven Consumer** | RabbitMQ message | Status changed, sprint started |
| **Scheduled (Cron)** | Time-based | Daily burndown, weekly velocity |
| **On-demand** | Manual or API trigger | Backfill, re-generate report |

### When to use Consumer vs Scheduled Job

| Scenario | Consumer | Scheduled |
|---|---|---|
| Must happen immediately after event | ✅ | ❌ |
| Order matters | ✅ | ❌ |
| Trigger time unknown | ✅ | ❌ |
| Fixed time schedule | ❌ | ✅ |
| Scan entire dataset | ❌ | ✅ |
| Takes >5 seconds | Enqueue job | ✅ |
| Cannot run concurrently | ✅ (concurrency=1) | ✅ (DisallowConcurrent) |
| OK to miss occasionally | ❌ | ✅ (DoNothing misfire) |
| Critical — must not miss | ✅ | ✅ (FireNow misfire) |

---

## Phase Rollout

| Phase | Jobs Activated |
|---|---|
| **0** | Infrastructure only — Quartz, MassTransit, DLQ, metrics table. No real jobs. |
| **1** | `WorkItemStatusChangedConsumer`, `WorkItemCreatedConsumer`, `EventPartitionCreatorJob` |
| **2** | `WorkItemRejectedConsumer`, `WorkItemHistoryConsumer` |
| **3** | `SprintStartedConsumer`, `SprintCompletedConsumer`, `BurndownSnapshotJob`, `ReleaseOverdueDetectorJob`, `StaleItemDetectorJob`, `SprintReportGeneratorJob`, Email consumers |
| **4** | `RetroSessionClosedConsumer`, `RetroActionItemLinkerConsumer` |
| **5** | `EmailOutboxProcessorJob`, `DeadlineReminderJob`, `VelocityAggregatorJob`, `SprintReportGeneratorJob` (enhanced), `DataArchivalJob`, `TeamHealthSummaryJob`, `EmbeddingRefreshJob`, `WorkItemAssignedNotificationConsumer`, `NotificationCreatedConsumer` |

---

## Event-Driven Consumers

### WorkItemStatusChangedConsumer
**Queue:** `workitem.status_changed`

```
1. Persist DomainEvent with full payload + context
2. Update BurndownDataPoints if item in active sprint
3. Check blocker cascade — if status → Done:
   → find all items "blocked by" this item
   → publish WorkItem.BlockerResolved for each
   → broadcast SignalR to update blocked icons
4. Mark WorkItemEmbeddings.is_stale = true
5. If regression (Done → InProgress): flag sprint health degradation, notify TL
6. Broadcast SignalR: workitem.status_changed + sprint.progress_updated
```

### SprintStartedConsumer
**Queue:** `sprint.started`

```
1. Persist DomainEvent with full Sprint.Started payload
2. Create SprintSnapshot (type: OnStart, is_final: false)
3. Initialize BurndownDataPoints for today
4. Register daily BurndownSnapshotJob for this sprint in Quartz
5. Broadcast SignalR: sprint.started to all project members
```

### SprintCompletedConsumer
**Queue:** `sprint.completed`

```
1. Persist DomainEvent
2. Finalize SprintSnapshot (is_final: true) — immutable after this
3. Update TeamVelocityHistory — recalculate rolling averages
4. Enqueue SprintReportGeneratorJob (async, separate)
5. Unschedule daily BurndownSnapshotJob for this sprint
6. Update carried items: sprint_id = null, publish WorkItem.CarriedOver
7. Broadcast SignalR: sprint.completed
```

### RetroSessionClosedConsumer
**Queue:** `retro.session_closed`

```
1. Persist DomainEvent with sprint_metrics_snapshot
2. Process Action Items linked to backlog:
   → Create WorkItem (Task, tag: retro-action)
   → Set RetroActionItems.linked_task_id
   → Publish WorkItem.Created
3. Check repeated themes vs last 2 retros:
   → If same theme 3× → create Alert notification for PO + TL
4. Broadcast SignalR: retro.session_closed, retro.summary_ready
```

---

## Scheduled Jobs

### BurndownSnapshotJob
**Cron:** `59 23 * * ?` (11:59 PM daily)  
**Priority:** High | **Misfire:** FireNow | **Concurrent:** No

```
For each active sprint:
  1. Query: total items, Done items, remaining points, completed points, scope changes today
  2. Calculate ideal burndown line
  3. Insert BurndownDataPoints record
  4. If actual_remaining > ideal * 1.2 → flag "At Risk", notify TL + Team Manager
  5. Broadcast SignalR: burndown.updated

Performance: <500ms per sprint
Isolation: each sprint in separate DB transaction
Concurrency: semaphore limit 5 simultaneous sprints
```

### ReleaseOverdueDetectorJob
**Cron:** `5 0 * * ?` (00:05 AM daily)  
**Priority:** High | **Misfire:** FireNow

```
Query: releases WHERE status = 'Unreleased' AND release_date < TODAY

For each overdue release:
  1. Compute: incomplete_items, incomplete_points, estimated_days_to_complete
  2. Update release status → Overdue
  3. Persist DomainEvent: Release.OverdueDetected
  4. Create Notifications → PO and TL
  5. Enqueue email (via email worker queue)
  6. Broadcast SignalR: release.overdue_detected
```

### StaleItemDetectorJob
**Cron:** `0 8 * * ?` (08:00 AM daily)  
**Priority:** Medium | **Misfire:** DoNothing

```
Query: work_items WHERE status NOT IN (Done, Rejected) AND updated_at < NOW() - 14 days

For each stale item:
  1. Check: already flagged in last 14 days? Skip if yes.
  2. Calculate severity:
     InSprint + NearRelease  → Critical (alert TL)
     InSprint only           → High (alert assignee)
     HasRelease              → Medium (alert assignee)
     InBacklog only          → Low (no alert)
  3. Persist DomainEvent: WorkItem.StaleFlagged
  4. Create in-app Notification
  5. Update ai_metadata.stale_flag = true (UI shows ⚠)

Skip: archived projects, items assigned within 14 days
```

### VelocityAggregatorJob
**Cron:** `0 7 * * 1` (07:00 AM Monday)  
**Priority:** Low | **Misfire:** DoNothing

```
For each project with completed sprints:
  1. Recalculate: velocity_3sprint_avg, velocity_6sprint_avg, trend (linear regression)
  2. Compute confidence intervals (std deviation of last 6 sprints)
  3. Detect anomaly: latest velocity < lower bound → flag + notify TL
  4. Update TeamVelocityHistory
  5. Update project sprint recommendation:
     recommended_points = velocity_3sprint_avg * 0.85  (conservative 85%)
```

### EmbeddingRefreshJob
**Cron:** `0 2 * * ?` (02:00 AM daily)  
**Priority:** Low | **Misfire:** DoNothing  
**Active:** Phase 5 only

```
Query: work_item_embeddings WHERE is_stale = true LIMIT 100

For each stale item:
  1. Build text: title + description + acceptance_criteria + tags
  2. Call AI embedding API (with circuit breaker)
  3. Update WorkItemEmbeddings: embedding, is_stale=false, generated_at, model
  4. Sleep 100ms between items (rate limit)

Circuit breaker: >50% calls fail in 5 min → skip job, alert team, retry after 4h
If >100 stale remain → schedule rerun after 1 hour
```

### EventPartitionCreatorJob
**Cron:** `0 3 25 * ?` (03:00 AM, 25th of month)  
**Priority:** Critical | **Misfire:** FireNow

```
1. Create next month's partition for domain_events
2. Verify partition created successfully
3. Alert team if fails — this is critical (missing partition = insert failures)
```

### DataArchivalJob
**Cron:** `0 3 1 * ?` (03:00 AM, 1st of month)  
**Priority:** Low | **Misfire:** FireNow  
**Active:** Phase 5

```
Phase 1: Identify candidates
  - DomainEvents older than 36 months
  - ExternalEvents older than 12 months
  - BurndownDataPoints older than 24 months
  - Soft-deleted WorkItems older than 30 days → hard delete

Phase 2: Export to cold storage
  - Export to gzipped JSON lines
  - Upload to S3: archives/{table}/{year}/{month}.json.gz
  - Verify checksum

Phase 3: Drop old partition (DomainEvents)
  - ONLY after S3 upload verified
  - Never drop without verification

Phase 4: Hard delete soft-deleted WorkItems
  - DELETE WHERE deleted_at < NOW() - 30 days
  - WorkItemHistories NOT cascade deleted — orphan but preserved
```

### SprintReportGeneratorJob
**Trigger:** Enqueued by SprintCompletedConsumer  
**Timeout:** 2 minutes

```
1. Gather data (parallel queries):
   - SprintSnapshot (is_final=true)
   - All DomainEvents for this sprint
   - All WorkItems in sprint
   - TeamVelocityHistory (6 prev sprints)
   - BurndownDataPoints for sprint
   - RetroActionItems from previous retro

2. Compute derived metrics:
   - Predictability score
   - Quality score (bugs, rework, rejections)
   - Blocker impact (hours blocked)

3. Generate report (template-based for Phase 3, AI-enhanced in Phase 5)

4. Store in sprint_reports table

5. Send email to PO, TL, Team Manager

6. Broadcast SignalR: sprint.report_ready
```

### EmailOutboxProcessorJob
**Cron:** `*/30 * * * * ?` (every 30 seconds)
**Priority:** High | **Misfire:** FireNow | **Concurrent:** No
**Active:** Phase 5

```
Query: email_outbox WHERE status IN ('Pending', 'Failed') AND next_retry_at <= NOW()

For each email:
  1. Set status → Sending
  2. Attempt send via IEmailSender (SMTP/MailKit)
  3. On success: set status → Sent, set sent_at
  4. On failure: increment attempt_count
     - If attempt_count < max_attempts:
       Set status → Failed, set next_retry_at with exponential backoff (30s, 5m, 30m)
     - If attempt_count >= max_attempts:
       Set status → DeadLettered, log alert

Performance: <100ms per email attempt
Isolation: each email in separate DB transaction
```

### DeadlineReminderJob
**Cron:** `0 8 * * ?` (08:00 AM daily)
**Priority:** Medium | **Misfire:** DoNothing
**Active:** Phase 5

```
Query: work_items with release date 1 day or 3 days away
  AND status NOT IN (Done, Rejected)
  AND assignee is set

For each matching item:
  1. Check user's notification preferences for DeadlineReminder1d / DeadlineReminder3d
  2. If enabled: create in-app notification via INotificationService
  3. If email enabled: create EmailOutbox entry for email delivery
  4. Skip items already reminded for this deadline window

Skip: archived projects, unassigned items
```

### TeamHealthSummaryJob
**Cron:** `0 7 * * 1` (07:30 AM Monday — runs after VelocityAggregatorJob)
**Priority:** Low | **Misfire:** DoNothing
**Active:** Phase 5

```
For each project with completed sprints:
  1. Aggregate weekly metrics:
     - Velocity trend (from TeamVelocityHistory)
     - Bug rate (bugs created / total items)
     - Rework rate (items moved back to earlier status)
     - Stale item count
     - Average cycle time
     - Sprint predictability score
  2. Store in team_health_summaries table
  3. Send summary email to TL + Team Manager
  4. Broadcast SignalR: team_health.summary_ready
```

---

## Job Infrastructure

### Graceful Shutdown
```
Shutdown signal received
  → Stop accepting new RabbitMQ messages
  → Finish current consumer messages (timeout: 30s)
  → Signal CancellationToken to running jobs
  → Wait for jobs to reach checkpoint (timeout: 60s)
  → Flush metrics, close DB connections
  → Exit
```

Jobs use checkpoint pattern:
```csharp
foreach (var item in items) {
    cancellationToken.ThrowIfCancellationRequested();
    // process item
    // commit transaction per item
}
```

### Job Metrics Table
```sql
CREATE TABLE job_execution_metrics (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  job_type          VARCHAR(100) NOT NULL,
  job_run_id        UUID NOT NULL,
  status            VARCHAR(20) NOT NULL, -- Running, Success, Failed, Cancelled
  started_at        TIMESTAMPTZ NOT NULL,
  completed_at      TIMESTAMPTZ,
  duration_ms       INTEGER,
  records_processed INTEGER DEFAULT 0,
  records_failed    INTEGER DEFAULT 0,
  error_message     TEXT,
  metadata          JSONB DEFAULT '{}'::jsonb
);
```

### Alerting Rules
- Job failed → immediate alert
- Job duration > 2× average → warning
- Job skipped >3 times → alert (DoNothing misfire)
- DLQ message count >10 → immediate alert
- EventPartitionCreatorJob failed → critical alert (will cause insert failures)
