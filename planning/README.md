# TeamFlow — Product Planning

> Internal project management platform for engineering teams of 9–15 people.  
> Inspired by Azure DevOps and Jira — leaner, faster, AI-ready.

---

## Quick Navigation

| Document | Description |
|---|---|
| [01-vision.md](./01-vision.md) | Product vision, core principles, tech decisions |
| [02-roles-permissions.md](./02-roles-permissions.md) | 6 roles, permission matrix, 3-level system |
| [03-data-model.md](./03-data-model.md) | Database schema, AI-ready tables, retention strategy |
| [04-features.md](./04-features.md) | Full feature inventory — work items, linking, release, retro |
| [05-phases.md](./05-phases.md) | 5-phase breakdown with scope and acceptance criteria |
| [06-events.md](./06-events.md) | Domain event catalog, SignalR + RabbitMQ architecture |
| [07-background-jobs.md](./07-background-jobs.md) | Background job design and rollout per phase |
| [08-definition-of-done.md](./08-definition-of-done.md) | DoD, cross-phase rules, risk register |
| [09-future-roadmap.md](./09-future-roadmap.md) | AI roadmap, 6-month / 1-year / 2-year outlook |
| [CLAUDE.md](./CLAUDE.md) | Instructions for Claude Code — architecture, conventions, commands |

---

## Revised Timeline (Claude Code assisted)

```
Phase 0  Weeks 1–2   Foundation & Design Ready
Phase 1  Weeks 3–5   Work Item Management + Kanban + Linking + Release
Phase 2  Weeks 6–8   Auth + Permission + History
Phase 3  Weeks 9–12  Hardening + Sprint Planning + MVP Release
Phase 4  Weeks 13–16 Collaboration + Planning Poker + Retrospective
Phase 5  Weeks 17–20 Analytics + Notifications + Automation
```

**Total: 20 weeks** (down from 28 with Claude Code assistance)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js (App Router) · TanStack Query · Zustand |
| API | .NET 8 · Controller-based · Clean Architecture · Vertical Slice |
| Database | PostgreSQL · EF Core · Npgsql |
| Broker | RabbitMQ · MassTransit |
| Realtime | SignalR (ASP.NET Core) |
| Background | .NET Hosted Services · Quartz.NET |
| Auth | JWT · Refresh Token |
| Rate Limiting | .NET 7+ built-in |
| Testing | xUnit · Testcontainers |
| Local Dev | Docker Compose |

---

## Milestones

| Milestone | Week | Gate |
|---|---|---|
| M0 — Foundation Ready | Week 2 | Both tracks unblocked |
| M1 — Core CRUD | Week 5 | Feature-complete |
| M2 — Secure | Week 8 | No open endpoints |
| M3 — MVP | Week 12 | Production-ready + dogfooded |
| M4 — Collaboration | Week 16 | Full Scrum cycle in TeamFlow |
| M5 — Full Product | Week 20 | All 5 phases complete |
