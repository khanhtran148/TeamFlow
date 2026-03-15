# 05 — Phase Breakdown

> Timeline revised for Claude Code assisted development.  
> Estimated speed increase: ~40–60% for boilerplate, ~20–30% for complex logic.

---

## Timeline Overview

```
Phase 0  Weeks 1–2    Foundation & Design Ready          2 weeks
Phase 1  Weeks 3–5    Work Item + Kanban + Linking        3 weeks
Phase 2  Weeks 6–8    Auth + Permission + History         3 weeks
Phase 3  Weeks 9–12   Hardening + Sprint Planning + MVP   4 weeks
Phase 4  Weeks 13–16  Collaboration + Retro               4 weeks
Phase 5  Weeks 17–20  Analytics + Notifications           4 weeks
─────────────────────────────────────────────────────────────────
Total    20 weeks
```

---

## 🔵 Phase 0 — Foundation & Design Ready
**Weeks 1–2**  
**Goal:** Both tracks unblocked and ready to build Phase 1 on day 1.  
**Claude Code role:** Scaffold project structure, entities, migrations. Human reviews and adjusts.

### Frontend Track
- [ ] Design system: color tokens (dark + light), typography, spacing, component library
- [ ] Dark/Light mode — token-based, persistent, zero flicker
- [ ] Layout shell: sidebar, topbar, breadcrumb
- [ ] Prototype screens (clickable, both modes):
  - Backlog & Sprint Planning
  - Kanban Board
  - Work Item Detail (with Links + History tabs)
  - Project Overview
  - Team Management
  - Release List & Detail
  - Retro Session
- [ ] Next.js routing, page shells created
- [ ] TanStack Query, Zustand store, Axios interceptor with JWT refresh
- [ ] SignalR client: connect, reconnect, disconnect lifecycle

### Backend Track
- [ ] Solution structure confirmed and locked
- [ ] All enums defined (ProjectRole, WorkItemType, WorkItemStatus, etc.)
- [ ] Full database schema — all tables including AI-ready tables:
  - Core: Users, Organizations, Teams, TeamMembers, Projects, ProjectMemberships
  - Work: WorkItems, WorkItemHistories, WorkItemLinks
  - Sprint/Release: Sprints, Releases
  - Retro: RetroSessions, RetroCards, RetroVotes, RetroActionItems
  - AI-ready: DomainEvents (partitioned), SprintSnapshots, BurndownDataPoints, TeamVelocityHistory, WorkItemEmbeddings, AIInteractions
- [ ] EF Core migration — applies cleanly from zero
- [ ] Seed data: 6 user accounts (one per role), sample project, epics, stories, tasks
- [ ] Test infrastructure: xUnit, Testcontainers, test data builders
- [ ] API conventions locked: versioning, ProblemDetails, pagination, date format
- [ ] RabbitMQ + management UI in Docker Compose
- [ ] SignalR Hub skeleton — connection setup only
- [ ] Background Service skeleton — IHostedService shell + Quartz.NET setup
- [ ] MassTransit configuration skeleton
- [ ] Event contract document published (see `06-events.md`)
- [ ] Swagger/OpenAPI auto-generated
- [ ] Health check at `/health`
- [ ] CI/CD: build + test on every PR
- [ ] `docker compose up` starts full stack
- [ ] CLAUDE.md written (see `CLAUDE.md`)

### Acceptance Criteria
- [ ] `docker compose up` → full stack running, no errors, no manual steps
- [ ] Schema migrates from zero cleanly; seed data loads
- [ ] New developer running local within 15 minutes of cloning
- [ ] CI green on every PR, merge blocked if red
- [ ] All prototype screens clickable in both dark and light mode
- [ ] Dark/Light toggle instant, persists across reload, no flicker
- [ ] Component library covers all Phase 1 UI needs
- [ ] SignalR handshake from frontend to API completes
- [ ] Dummy event completes full loop: API → RabbitMQ → Background Service → SignalR → Frontend
- [ ] Frontend and Backend teams agreed on Phase 1 API contracts

---

## 🟢 Phase 1 — Work Item Management
**Weeks 3–5 (3 weeks)**  
**Goal:** Core CRUD + Kanban + Linking + Release basics working end-to-end. Seed users — no auth enforcement yet.  
**Claude Code role:** Generate handlers, validators, controllers, integration tests per feature slice.

### Scope
- [ ] **Project CRUD** — create, edit, archive, delete; list with filter/search
- [ ] **Work Item Hierarchy** — Epic → Story → Task/Bug/Spike full CRUD
  - Parent-child enforced
  - Soft delete with cascade
  - All mutations write WorkItemHistories automatically
- [ ] **Assign / Unassign** — single assignee, history records old/new
- [ ] **Backlog View** — hierarchy grouped, filter, search, reorder, release badge, blocked icon
- [ ] **Kanban Board** — drag-drop status update, swimlanes, quick edit, blocked icon
- [ ] **Item Linking** — all 6 types, bidirectional auto-create, circular detection, cross-project
  - Blocked confirm dialog when moving to In Progress
  - Link events recorded in history of both items
- [ ] **Release Basics** — create/edit/delete release, assign items, badge on backlog/detail
- [ ] **Realtime** — all actions publish to RabbitMQ → SignalR → clients update

### Acceptance Criteria
- [ ] Full chain: Project → Epic → Story → Task — works end-to-end
- [ ] Delete Epic → all children soft-deleted, history preserved
- [ ] Assign → displayed on Backlog and Detail immediately
- [ ] Unassign → unassigned state, no other field affected
- [ ] Create A blocks B → B shows "is blocked by A" automatically
- [ ] Delete link from A → reverse disappears from B
- [ ] Circular block attempt → API returns clear error
- [ ] Blocked item moved → confirm dialog lists blockers
- [ ] Release badge appears on Backlog row in real-time after assignment
- [ ] Each endpoint has: happy path + validation error + not found tests
- [ ] No endpoint returns 500 on valid input
- [ ] Both dark and light mode render correctly
- [ ] Two browser tabs: change in one → other updates without refresh

---

## 🟡 Phase 2 — Authentication & Authorization
**Weeks 6–8 (3 weeks)**  
**Goal:** System knows who you are and enforces what you can do on all Phase 1 endpoints.  
**Claude Code role:** Auth flow is standard pattern — scaffold fully. Permission resolver logic needs careful human review.

### Scope
- [ ] **Authentication**
  - Register (email + bcrypt password)
  - Login → JWT access token + Refresh Token
  - Silent refresh — no mid-session logout
  - Change password
  - Logout → server-side refresh token revoke
  - Rate limit: 10 req / 15 min per IP
- [ ] **Team Management**
  - Create Team, add/remove members
  - Assign Team Manager role
  - Team Manager manages own team only
  - Project membership: assign Team or User with role
- [ ] **Permission System — 3 Levels**
  - Organization, Team, Individual resolution order
  - Permission middleware on all Phase 1 endpoints
  - PO and Tech Lead scoped per-project
- [ ] **Work Item History UI**
  - History tab in Work Item Detail
  - Chronological feed, newest first
  - Actor avatar + name + action description + relative timestamp
  - Visual distinction by action type
  - Rejection reason displayed in history entry
  - Pagination for 500+ entries
  - Realtime: history_added event updates tab live

### Acceptance Criteria
- [ ] Register → Login → JWT → call protected API → success
- [ ] Token expires + valid refresh → new token, no logout
- [ ] Viewer calls POST /workitems → 403
- [ ] Developer deletes Project → 403
- [ ] Team Manager manages own team → success; other team → 403
- [ ] Individual override: Developer granted Tech Lead on one project → resolves correctly
- [ ] Org Admin never receives 403
- [ ] Auth endpoint 11th request in 15 min → 429 with Retry-After header
- [ ] PO: no vote button in refinement, cannot start sprint
- [ ] Tech Lead: can close Task, can flag Story
- [ ] Every mutation generates exactly one history record with correct values
- [ ] History survives soft-delete of parent item
- [ ] No history modifiable via any endpoint including Org Admin

---

## 🟠 Phase 3 — Hardening + Sprint Planning + MVP Release
**Weeks 9–12 (4 weeks)**  
**Goal:** Production-worthy. Sprint Planning added so team can dogfood with real workflow. No new features beyond sprint planning.  
**Claude Code role:** Test generation, Sprint Planning handlers. Human focuses on quality review and production setup.

### Scope
- [ ] **Sprint Planning**
  - Create Sprint: goal, dates, per-member capacity
  - Move items from backlog → capacity indicator live
  - Capacity warning when points exceed capacity
  - Start Sprint → scope locked, additions need Team Manager confirmation
  - Realtime: sprint events broadcast
- [ ] **Sprint-related Background Jobs**
  - `BurndownSnapshotJob` — 11:59 PM daily, captures remaining/completed points
  - `ReleaseOverdueDetectorJob` — 00:05 AM daily, transitions + notifies PO + TL
  - `StaleItemDetectorJob` — 08:00 AM daily, flags 14-day-stale items
  - `EventPartitionCreatorJob` — 25th of month, creates next month's partition
- [ ] **Test Coverage** — audit gaps, edge cases, ≥70% Application layer
- [ ] **Performance** — backlog 1000 items <500ms, indexes, pagination
- [ ] **Bug Fix & UX Polish** — 1 week dogfooding, all P0/P1 resolved
- [ ] **Observability** — Serilog structured logging, health check per dependency, error monitoring
- [ ] **Production Readiness** — env config separated, zero secrets in code, deploy zero downtime

### Acceptance Criteria
- [ ] Sprint created → items moved → capacity indicator correct
- [ ] Sprint started → scope locked → addition needs confirmation
- [ ] Burndown data written daily at 11:59 PM for active sprints
- [ ] Overdue release detected within 24h of date passing, PO + TL notified
- [ ] Stale item ⚠ appears on board after 14 days no update
- [ ] Zero P0/P1 bugs at release gate
- [ ] Application layer coverage ≥70%
- [ ] Backlog 1000 items loads + filters <500ms
- [ ] No endpoint >1 second under normal load
- [ ] 1 week dogfooding — zero crashes, zero data loss
- [ ] Logs sufficient to debug any production bug without local reproduction
- [ ] Production deploy zero downtime
- [ ] Health check correctly reports degraded when Postgres or RabbitMQ down
- [ ] Lighthouse score ≥80 on main screens

---

## 🔴 Phase 4 — Collaboration & Planning
**Weeks 13–16 (4 weeks)**  
**Goal:** Full Scrum cycle runnable in TeamFlow — comments, poker, retro, release detail.  
**Claude Code role:** Comment and retro logic, Planning Poker state machine.

### Scope
- [ ] **Comment System**
  - Comment on Epic, Story, Task
  - @mention → in-app notification
  - Edit/delete own comment
  - Basic thread/reply
  - Realtime: comment.created broadcasts
- [ ] **Planning Poker**
  - Session per User Story
  - Fibonacci votes (1,2,3,5,8,13,21)
  - PO: observer only — no vote button
  - Votes hidden until facilitator reveals all simultaneously
  - Tech Lead / Team Manager confirms final value
  - Realtime: vote count updates live
- [ ] **Backlog Refinement**
  - Mark items "Ready for Sprint"
  - Bulk update priority
  - Filter: blocked only / ready only
- [ ] **Retrospective** — full feature (see `04-features.md` for rules)
  - Session lifecycle: Draft → Open → Voting → Discussing → Closed
  - Anonymous/Public mode
  - Dot voting
  - Action Items with optional backlog Task link
  - Previous session Action Items shown
  - Auto-generated summary on close
  - Realtime for all retro events
- [ ] **Release Detail Page**
  - Progress: Done / In Progress / To Do counts + points
  - Grouped views: by Epic, by Assignee, by Sprint
  - Release notes editable by PO + TL before ship
  - Confirm dialog when releasing with open items
  - Overdue highlighted in red

### Acceptance Criteria
- [ ] Comment visible to all session viewers without reload
- [ ] @mention generates notification
- [ ] PO has no vote button in Planning Poker
- [ ] Votes hidden until facilitator reveals
- [ ] Vote count updates live (count only, not value)
- [ ] Retro anonymous: no names visible to members
- [ ] Anonymity locked once session opens
- [ ] Retro Action Item linked to backlog → Task created with retro-action tag
- [ ] Previous Action Items shown at top of new session
- [ ] Retro summary accessible after close — Viewer can read
- [ ] Release with open items: confirm dialog lists all incomplete items
- [ ] All Phase 4 features do not degrade Phase 1–3 response times

---

## 🟣 Phase 5 — Insights & Automation
**Weeks 17–20 (4 weeks)**  
**Goal:** Leadership has data. System automates reporting and reminders.  
**Claude Code role:** Dashboard queries, notification templates, background job handlers — all high-value Claude Code targets.

### Scope
- [ ] **Dashboard & Analytics**
  - Velocity chart — last N sprints
  - Burn-down chart — realtime active sprint
  - Cumulative flow diagram
  - Cycle time per item type
  - Team workload heatmap
  - Release progress dashboard
- [ ] **Notifications & Reminders**
  - Email on work item assignment
  - Deadline reminder: configurable (1 day / 3 days before)
  - Sprint summary email on sprint close
  - Release overdue email to PO + TL
  - In-app notification center: read/unread, mark all read
  - Per-user preferences: enable/disable per type
  - Failed delivery: exponential backoff × 3 → dead-letter queue + alert
- [ ] **Background Automation**
  - `SprintReportGeneratorJob` — auto on sprint close
  - `VelocityAggregatorJob` — Monday 07:00 AM
  - `DataArchivalJob` — 1st of month 03:00 AM
  - `TeamHealthSummaryJob` — weekly
- [ ] **Advanced Search**
  - Full-text search: PostgreSQL tsvector + GIN index
  - Multi-condition filter combinations
  - Saved filters per user

### Acceptance Criteria
- [ ] Burn-down updates within 30 seconds of status change
- [ ] Email delivered within 1 minute of trigger
- [ ] Failed email retries × 3 with backoff → dead-letter queue
- [ ] Sprint report auto-generated at sprint close
- [ ] Release overdue detected within 24h → email sent
- [ ] Cleanup job runs without API performance impact (off-peak)
- [ ] Velocity chart correct across ≥5 completed sprints
- [ ] Notification preference respected — disabled type receives nothing
- [ ] Full-text search 1000 items <300ms
- [ ] Saved filter persists across sessions

---

## Cross-Phase Rules

1. **No new features added to a phase in progress** — log to backlog for next phase
2. **Bugs from previous phases take priority** over new feature work
3. **Each phase ends with a demo** — team confirms all ACs before moving on
4. **All migrations backward compatible** — never DROP column in same deploy as code change
5. **API contracts don't change without version bump** and both-team agreement
6. **No secrets in source control** — ever, on any branch
7. **All realtime features have REST fallback** — SignalR is UX enhancement, not correctness dependency
8. **WorkItemHistories is append-only** — no UPDATE or DELETE queries against this table

---

## Definition of Done

A feature is Done only when ALL of the following are true:

- [ ] Unit test: happy path + at least one edge case per handler
- [ ] API returns ProblemDetails (400) for all invalid inputs
- [ ] Rate limiting applied with correct policy
- [ ] Permission check enforced — 403 for unauthorized
- [ ] No breaking API change without version bump
- [ ] PR reviewed and approved by ≥1 developer
- [ ] Feature runs without error on Dev environment
- [ ] No secrets or env values in committed code
- [ ] Realtime events publishing correctly — verified in RabbitMQ UI
- [ ] History records written correctly — verified by integration test
