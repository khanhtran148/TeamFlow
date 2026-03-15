# Domain Events & Realtime Architecture

## Event Envelope — All Events Share This Structure

```json
{
  "event_id":       "uuid",
  "event_type":     "WorkItem.StatusChanged",
  "schema_version": 1,
  "aggregate_type": "WorkItem",
  "aggregate_id":   "uuid",
  "occurred_at":    "2026-03-15T09:23:11.000Z",
  "recorded_at":    "2026-03-15T09:23:11.043Z",
  "actor": {
    "id":   "uuid",
    "type": "User",
    "role": "Developer",
    "name": "Hieu N."
  },
  "session": {
    "id":             "uuid",
    "correlation_id": "uuid",
    "causation_id":   "uuid"
  },
  "payload": { }
}
```

**`correlation_id`** — groups all events from one user action
**`causation_id`** — which event triggered this event
**`recorded_at` != `occurred_at`** — supports event replay and external imports

---

## Event Registry

| Event Type | Phase | AI Priority | SignalR Broadcast |
|---|---|---|---|
| WorkItem.Created | 1 | Medium | Yes |
| WorkItem.StatusChanged | 1 | **High** | Yes |
| WorkItem.EstimationChanged | 1 | **High** | Yes |
| WorkItem.Assigned | 1 | **High** | Yes |
| WorkItem.Unassigned | 1 | Medium | Yes |
| WorkItem.PriorityChanged | 1 | Medium | Yes |
| WorkItem.LinkAdded | 1 | Medium | Yes |
| WorkItem.LinkRemoved | 1 | Low | Yes |
| WorkItem.Rejected | 1 | **High** | Yes |
| WorkItem.NeedsClarificationFlagged | 1 | Medium | Yes |
| WorkItem.StaleFlagged | 3 | Medium | Yes |
| WorkItem.HistoryAdded | 1 | Low | Yes |
| Sprint.Started | 3 | **High** | Yes |
| Sprint.Completed | 3 | **High** | Yes |
| Sprint.ItemAdded | 3 | **High** | Yes |
| Sprint.ItemRemoved | 3 | Medium | Yes |
| Release.Created | 1 | Low | Yes |
| Release.ItemAssigned | 1 | Low | Yes |
| Release.StatusChanged | 1 | **High** | Yes |
| Release.OverdueDetected | 3 | **High** | Yes |
| Retro.SessionStarted | 4 | Medium | Yes |
| Retro.CardSubmitted | 4 | Low | Yes (count only) |
| Retro.CardsRevealed | 4 | Medium | Yes |
| Retro.VoteCast | 4 | Low | Yes |
| Retro.ActionItemCreated | 4 | Medium | Yes |
| Retro.SessionClosed | 4 | **High** | Yes |
| Notification.Created | 5 | Low | Yes |
| AI.SuggestionPresented | Future | **High** | No |
| AI.SuggestionActedOn | Future | **High** | No |

---

## Domain Event Classes (C#)

All event records live in `TeamFlow.Domain.Events`. Once published, their namespaces and class names are immutable.

### WorkItemDomainEvents

| Class | Properties |
|---|---|
| `WorkItemCreatedDomainEvent` | WorkItemId, ProjectId, Type, Title, ActorId |
| `WorkItemStatusChangedDomainEvent` | WorkItemId, ProjectId, FromStatus, ToStatus, SprintId?, ActorId |
| `WorkItemEstimationChangedDomainEvent` | WorkItemId, ProjectId, OldValue?, NewValue?, Source?, ActorId |
| `WorkItemAssignedDomainEvent` | WorkItemId, ProjectId, OldAssigneeId?, NewAssigneeId, ActorId |
| `WorkItemUnassignedDomainEvent` | WorkItemId, ProjectId, PreviousAssigneeId, ActorId |
| `WorkItemPriorityChangedDomainEvent` | WorkItemId, ProjectId, OldPriority?, NewPriority?, ActorId |
| `WorkItemLinkAddedDomainEvent` | WorkItemId, LinkedItemId, LinkType, ActorId |
| `WorkItemLinkRemovedDomainEvent` | WorkItemId, LinkedItemId, LinkType, ActorId |
| `WorkItemRejectedDomainEvent` | WorkItemId, ProjectId, RejectionReason?, ActorId |
| `WorkItemNeedsClarificationFlaggedDomainEvent` | WorkItemId, ProjectId, Notes?, ActorId |
| `WorkItemStaleFlaggedDomainEvent` | WorkItemId, ProjectId, Severity, DaysSinceUpdate |

### SprintDomainEvents

| Class | Properties |
|---|---|
| `SprintStartedDomainEvent` | SprintId, ProjectId, SprintName, Goal?, StartDate, EndDate, ActorId |
| `SprintCompletedDomainEvent` | SprintId, ProjectId, SprintName, PlannedPoints, CompletedPoints, ActorId |
| `SprintItemAddedDomainEvent` | SprintId, WorkItemId, ProjectId, ActorId |
| `SprintItemRemovedDomainEvent` | SprintId, WorkItemId, ProjectId, ActorId |

### ReleaseDomainEvents

| Class | Properties |
|---|---|
| `ReleaseCreatedDomainEvent` | ReleaseId, ProjectId, ReleaseName, ActorId |
| `ReleaseItemAssignedDomainEvent` | ReleaseId, WorkItemId, ProjectId, ActorId |
| `ReleaseStatusChangedDomainEvent` | ReleaseId, ProjectId, FromStatus, ToStatus, ActorId |
| `ReleaseOverdueDetectedDomainEvent` | ReleaseId, ProjectId, ReleaseName, ReleaseDate, IncompleteItemCount |

### RetroDomainEvents (Phase 4)

| Class | Properties |
|---|---|
| `RetroSessionStartedDomainEvent` | SessionId, ProjectId, SprintId?, FacilitatorId |
| `RetroCardSubmittedDomainEvent` | SessionId, CardId, AuthorId |
| `RetroCardsRevealedDomainEvent` | SessionId, ProjectId, CardCount, FacilitatorId |
| `RetroVoteCastDomainEvent` | SessionId, CardId, VoterId, VoteCount |
| `RetroActionItemCreatedDomainEvent` | SessionId, ActionItemId, Title, AssigneeId?, ActorId |
| `RetroSessionClosedDomainEvent` | SessionId, ProjectId, SprintId?, CardCount, ActionItemCount, FacilitatorId |

---

## Key Payload Schemas

### WorkItem.StatusChanged

```json
{
  "item_id":    "uuid",
  "item_type":  "UserStory",
  "item_title": "As a sender, I can drag-drop signature fields",

  "transition": {
    "from_status":                 "InProgress",
    "to_status":                   "InReview",
    "time_in_from_status_seconds": 172800,
    "is_forward_transition":       true,
    "is_regression":               false
  },

  "sprint_context": {
    "sprint_id":             "uuid",
    "sprint_name":           "Sprint 14",
    "days_elapsed":          7,
    "days_remaining":        5,
    "sprint_completion_pct": 0.58,
    "planned_points":        42
  },

  "item_context": {
    "story_points":          8,
    "active_blocker_count":  0,
    "assignee_id":           "uuid",
    "days_since_last_update": 1
  },

  "rejection_reason": null
}
```

### Sprint.Started

```json
{
  "sprint": {
    "id":            "uuid",
    "name":          "Sprint 14",
    "goal":          "Complete eSign core document signing flow",
    "start_date":    "2026-03-17",
    "end_date":      "2026-03-28",
    "working_days":  10
  },

  "scope": {
    "total_items":              18,
    "total_points":             42,
    "items_with_blockers":      2,
    "items_carried_from_prev":  2,
    "by_type": {
      "UserStory": { "count": 8,  "points": 29 },
      "Task":      { "count": 6,  "points": 10 },
      "Bug":       { "count": 4,  "points":  3 }
    }
  },

  "team": {
    "total_capacity":       56,
    "capacity_utilization": 0.75,
    "per_member": [
      {
        "member_id":       "uuid",
        "role":            "TechnicalLeader",
        "capacity_points": 16,
        "assigned_points": 13
      }
    ]
  },

  "historical_context": {
    "prev_sprint_velocity":       38,
    "avg_velocity_last_3":        36,
    "trend":                      "Increasing",
    "prev_sprint_completion_pct": 0.90
  }
}
```

### Sprint.Completed

```json
{
  "sprint_id":   "uuid",
  "sprint_name": "Sprint 14",

  "outcomes": {
    "planned_points":      42,
    "completed_points":    35,
    "completion_pct":      0.833,
    "velocity":            35,
    "scope_change_points": 5
  },

  "item_breakdown": {
    "completed":           15,
    "carried_to_next":     2,
    "rejected_by_po":      1,
    "added_mid_sprint":    2
  },

  "quality_signals": {
    "bugs_created_during_sprint":  3,
    "rework_items":                1,
    "rejected_stories":            1,
    "avg_story_cycle_time_days":   3.2,
    "blocked_time_pct":            0.12,
    "last_minute_completions":     3
  },

  "predictability_score": 0.77,
  "health_score":         0.71
}
```

### Retro.SessionClosed

```json
{
  "session_id":  "uuid",
  "sprint_id":   "uuid",

  "participation": {
    "invited_count":   5,
    "submitted_count": 5,
    "anonymous_mode":  true
  },

  "cards": {
    "total":             14,
    "went_well":          6,
    "needs_improvement":  5,
    "action_items":       3,
    "top_voted_card": {
      "category":   "NeedsImprovement",
      "vote_count": 8,
      "theme_tag":  "deployment_friction"
    }
  },

  "action_items": {
    "created":               3,
    "linked_to_backlog":     2,
    "from_previous_session": 2,
    "previous_completed":    1,
    "previous_not_completed":1
  },

  "sprint_metrics_snapshot": {
    "velocity":         35,
    "completion_pct":   0.833,
    "rejected_stories": 1,
    "bugs_created":     3,
    "predictability":   0.77
  }
}
```

---

## RabbitMQ Setup

### Exchanges

| Exchange | Type | Purpose |
|---|---|---|
| `teamflow.events` | Topic | Main exchange — all domain events |
| `teamflow.retry` | Topic | Retry exchange for failed messages |
| `teamflow.dlx` | Fanout | Dead letter exchange |

### Routing Keys

Pattern: `{aggregate_type}.{event_action}`

Examples:
- `workitem.status_changed`
- `workitem.assigned`
- `sprint.started`
- `sprint.completed`
- `sprint.item_added`
- `release.overdue_detected`
- `retro.session_closed`

### Queues

| Queue | Binds to | Consumer |
|---|---|---|
| `signalr.broadcast` | `teamflow.events` | SignalR broadcast service |
| `email.notifications` | `teamflow.events` | Email worker |
| `burndown.snapshot` | `sprint.started`, `sprint.completed` | Burndown job trigger |
| `domain.event.store` | `teamflow.events` (all) | DomainEvents table writer |
| `sprint.started` | `teamflow.events` | SprintStartedConsumer |
| `sprint.completed` | `teamflow.events` | SprintCompletedConsumer |
| `teamflow.retry.queue` | `teamflow.retry` | Retry handler |
| `teamflow.dlq` | `teamflow.dlx` | Dead letter monitor |

---

## SignalR Hub Structure

```csharp
// Hub groups — clients join on connection
public class TeamFlowHub : Hub
{
    // Groups a client joins:
    // - project:{projectId}     — all project events
    // - sprint:{sprintId}       — sprint board and burndown events
    // - workitem:{workItemId}   — item detail page events
    // - retro:{sessionId}       — retro session events
    // - user:{userId}           — personal notifications
}
```

### Client-side events by page

| Page | Listens for |
|---|---|
| Backlog | `workitem.*`, `release.*` |
| Kanban Board | `workitem.status_changed`, `workitem.assigned`, `sprint.item_added` |
| Work Item Detail | `workitem.*`, `workitem.history_added`, `workitem.link_added` |
| Sprint Planning | `sprint.*`, `workitem.moved_to_sprint` |
| Release Detail | `release.*`, `workitem.release_assigned` |
| Retro Session | `retro.*` |
| Notification Center | `notification.created` |

The `BurndownSnapshotJob` broadcasts `burndown.updated` directly to `sprint:{sprintId}` after writing each daily snapshot.

---

## MassTransit Consumer Pattern

```
BaseConsumer<TMessage>
├── Pre-consume: log correlation_id, start metrics timer, validate schema_version
├── Consume (override): business logic
└── Post-consume: record to DomainEvents, stop timer, broadcast SignalR, on error → retry
```

### Retry Policy

```
Attempt 1  → immediate
Attempt 2  → 30 seconds
Attempt 3  → 5 minutes
Attempt 4  → 30 minutes
Attempt 5  → Dead Letter Queue + alert
```

### Idempotency

Check `event_id` in `processed_event_ids` set before processing.
Same event arriving twice → skip second occurrence.
