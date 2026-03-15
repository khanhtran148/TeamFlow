# 04 — Feature Inventory

## MoSCoW Prioritization

| Priority | Features |
|---|---|
| **Must Have** | Auth, Work Item CRUD, Backlog, Sprint Planning, Kanban Board, 6 Roles, Permission 3-levels, Work Item History, Item Linking (6 types), Release Management, Realtime via SignalR |
| **Should Have** | Dashboard & Analytics, Burn-down chart, Email Notifications, Retrospective, Full-text Search |
| **Could Have** | Report Scheduler, Saved Filters, Sprint Risk Warning, Release Notes auto-generation |
| **Won't Have v1** | AI features, Git integration, Time tracking, Mobile app, i18n, SSO/OAuth2 |

---

## Work Item Hierarchy

```
Project
└── Epic
    └── User Story
        └── Task / Bug / Spike
```

### Item Types & Fields

| Field | Epic | User Story | Task / Bug / Spike |
|---|---|---|---|
| Title | ✅ | ✅ | ✅ |
| Description | ✅ | ✅ | ✅ |
| Acceptance Criteria | ❌ | ✅ | ❌ |
| Story Points | ❌ | ✅ | ✅ |
| Priority | ✅ | ✅ | ✅ |
| Status | ✅ | ✅ | ✅ |
| Assignee | ❌ | ✅ | ✅ |
| Sprint | ❌ | ✅ | ✅ |
| Release | ✅ | ✅ | ✅ |
| Color Label | ✅ | ❌ | ❌ |
| Estimated Hours | ❌ | ❌ | ✅ |

---

## Item Linking — 6 Types

All links are bidirectional. Creating A → B auto-creates reverse B → A.

| Link Type | Forward | Reverse | Notes |
|---|---|---|---|
| Blocking | `blocks` | `is blocked by` | Circular detection enforced at API level |
| Relation | `relates to` | `relates to` | Symmetric — no workflow impact |
| Duplicate | `duplicates` | `is duplicated by` | Mark redundant items |
| Dependency | `depends on` | `is dependency of` | Softer than blocking |
| Causation | `causes` | `is caused by` | Bug tracing |
| Clone | `clones` | `is cloned by` | Copied item traceability |

### Blocked Item Behavior — Soft Warning

When item has unresolved `is blocked by` links:
- 🔴 icon on Kanban card and Backlog row
- Tooltip lists all active blockers
- Confirm dialog when moving to In Progress
- User can override — history records "moved despite active blockers"
- Circular blocking rejected at API with clear error

### Cross-Project Linking
- User chooses scope per link: Same Project or Cross-Project
- Cross-project: only items in projects where user has ≥ Viewer access appear in search

---

## Release Management

### Status Flow
```
Unreleased ──────────────────────► Released
    │                                  ▲
    │  (past release date + unfinished) │
    ▼                                  │
  Overdue ──────────────────────────►──┘
```

- **Overdue** auto-detected by daily background job
- **Released with open items** — confirm dialog required
- **Release notes** — auto-generated on ship, then locked permanently
- **Scope** — per-project only (no cross-project releases in v1)
- **Item constraint** — one item belongs to at most one release at a time

---

## Retrospective

### Session Lifecycle
```
Draft → Open → Voting → Discussing → Closed
```

### Key Rules
- Anonymity: configurable per session, locked once session opens
- Cards hidden until facilitator reveals (prevents anchoring)
- Dot voting: 5 votes per member, max 2 per card
- PO excluded from facilitating — can submit and vote cards
- Action Items: title + assignee + due date + optional backlog Task link
- Previous session's Action Items shown at top of new session
- Summary visible to all roles including Viewer permanently after close

---

## Sprint Planning

### Sprint Lifecycle
```
Planning → Active → Completed
```

- Capacity set per member when creating sprint
- Move items from backlog — capacity indicator live
- Warning when story points exceed team capacity
- Start Sprint locks scope — additions require Team Manager confirmation
- Sprint Goal defined by PO / Tech Lead / Team Manager

---

## Planning Poker (Refinement)

- Fibonacci scale: 1, 2, 3, 5, 8, 13, 21
- PO: observer only — no vote button, can add comments
- Votes hidden until facilitator reveals all simultaneously
- Tech Lead or Team Manager confirms final value
- History records estimation change with all votes snapshot

---

## Kanban Board

- Columns: To Do → In Progress → In Review → Done
- Drag-drop updates status (with blocked item confirm dialog)
- Swimlanes: by assignee or by epic
- Quick edit inline: assignee, points, priority
- Filter: by assignee, type, priority, sprint, release
- Blocked icon on cards with unresolved blockers

---

## Backlog View

- Grouped by Epic → Story → Task hierarchy
- Filter: status, priority, assignee, type, sprint, release
- "Unscheduled" filter: items not in any release
- Release badge on each item row
- Blocked icon 🔴 on items with unresolved blockers
- Reorder items within backlog
- Full empty state and loading skeleton

---

## Work Item History

Every mutation writes an immutable history record:
- Status changes
- Assignee changes
- Priority, story points, title, description edits
- Parent changes (moved between epics)
- Sprint / Release assignment changes
- Link added / removed
- Needs Clarification flagged
- Rejection with mandatory reason
- Soft delete

**Rules:**
- Append-only — no UPDATE or DELETE against history table
- Survives soft-delete of parent item
- No role (including Org Admin) can modify history
- Displayed as chronological feed, newest first
- Realtime: `workitem.history_added` event updates History tab live
