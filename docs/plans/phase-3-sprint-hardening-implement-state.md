# Implementation State — Phase 3: Sprint Planning + Hardening

## Topic
Phase 3: Hardening + Sprint Planning + MVP Release

## Discovery Context

- **Branch:** `feat/phase-3-sprint-hardening` (create new from main)
- **Requirements:** Phase 3 as defined in `docs/process/phases.md` and `docs/plans/phase-3-sprint-hardening/plan.md`
- **Test DB Strategy:** In-memory/mocks (NSubstitute for unit tests, Testcontainers for integration)
- **Feature Scope:** Fullstack
- **Task Type:** feature

## Phase-Specific Context

- **Plan directory:** `docs/plans/phase-3-sprint-hardening`
- **Plan source:** `docs/plans/phase-3-sprint-hardening/plan.md`
- **Discovery context:** `docs/plans/phase-3-sprint-hardening/discovery-context.md`

### Plan Summary

6 sub-phases with dependency chain: 3.0 → (3.1 || 3.2) → 3.3 → 3.4 → 3.5

| Sub-Phase | Goal | Size | Parallel |
|-----------|------|------|----------|
| 3.0 | API Contract + Domain Model fixes (seal entities, new interfaces, new builders) | M | No |
| 3.1 | Sprint Planning Backend (11 endpoints, CQRS handlers, validators, ~35 tests) | L | Yes (with 3.2) |
| 3.2 | Sprint Planning Frontend (Sprint pages, planning board, capacity, burndown chart) | M | Yes (with 3.1) |
| 3.3 | Background Jobs (4 Quartz jobs + 2 MassTransit consumers, ~20 tests) | L | After 3.1 |
| 3.4 | Hardening (test coverage >=70%, performance <500ms, observability, prod readiness) | XL | After 3.1+3.2 |
| 3.5 | Integration + Dogfooding (E2E tests, 1 week team usage, P0/P1 fixes) | L | After 3.3+3.4 |

### Key Codebase Facts
- Sprint entity, SprintStatus enum, SprintDomainEvents, SprintBuilder already exist from Phase 0
- BaseJob (Quartz) is implemented and ready, no jobs registered yet
- No Sprint feature slices exist in Application layer — all handlers need building
- ISprintRepository, IBurndownDataPointRepository do not exist yet
- Sprint entity needs `sealed` keyword added

### User Modifications
None — follow plan as-is.
