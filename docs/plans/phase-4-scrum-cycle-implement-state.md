# Phase 4 Implementation State

## Topic
Phase 4: Full Scrum Cycle — Comments, Planning Poker, Backlog Refinement, Retrospective, Release Detail Page

## Discovery Context

- **Branch:** `feat/phase-4-scrum-cycle` (created from main)
- **Feature Scope:** Fullstack (Frontend + Backend + API)
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)
- **Additional Requirements:** None — plan is complete as-is

## Phase-Specific Context

- **Plan Directory:** `docs/plans/phase-4-scrum-cycle`
- **Plan Source:** `docs/plans/phase-4-scrum-cycle/plan.md`
- **Discovery Context:** `docs/plans/phase-4-scrum-cycle/discovery-context.md`

### Plan Summary

7 sub-phases over 4 weeks:

| Sub-Phase | Scope | Dependencies |
|---|---|---|
| 4.0 | Schema & Infrastructure — new entities, migrations, permissions, repositories, builders | Blocks all |
| 4.1 | Comment System — CRUD, threads, @mentions, notifications, realtime | 4.0 |
| 4.2 | Retrospective — full lifecycle, anonymous mode, dot voting, action items | 4.0 |
| 4.3 | Planning Poker — Fibonacci votes, hidden reveal, PO observer, TL/TM confirm | 4.0, after 4.1 |
| 4.4 | Backlog Refinement — ready-for-sprint, bulk priority, filters | 4.0 |
| 4.5 | Release Detail — progress tracking, grouped views, editable notes, confirm dialog | 4.0 |
| 4.6 | Integration & E2E — cross-feature tests, perf regression, permission matrix | All above |

### Key Decisions (Pre-Confirmed)
- "Ready for Sprint" = boolean field on WorkItem
- Release notes = separate `release_notes` column
- PlanningPokerSession = unique constraint per work item (one active)
- Comment threading = one level deep
- @mention resolution at persist time in handler
- Retro anonymity enforced at handler level (strip AuthorId from DTO)
- Release ship = two-step confirm flow (409 → re-call with confirm flag)

### User Modifications
None — plan approved as-is.
