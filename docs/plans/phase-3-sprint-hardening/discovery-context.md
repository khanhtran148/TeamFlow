# Discovery Context — Phase 3: Hardening + Sprint Planning + MVP Release

## Requirements
Phase 3 as defined in `docs/process/phases.md` (Weeks 9-12). Goal: Production-worthy MVP with Sprint Planning so team can dogfood with real workflow. No new features beyond sprint planning.

### Scope Areas
1. **Sprint Planning** — Create sprint, capacity management, backlog-to-sprint flow, scope locking, realtime events
2. **Sprint-related Background Jobs** — BurndownSnapshotJob, ReleaseOverdueDetectorJob, StaleItemDetectorJob, EventPartitionCreatorJob
3. **Test Coverage** — Audit gaps, edge cases, >=70% Application layer
4. **Performance** — Backlog 1000 items <500ms, indexes, pagination
5. **Bug Fix & UX Polish** — 1 week dogfooding, all P0/P1 resolved
6. **Observability** — Serilog structured logging, health check per dependency, error monitoring
7. **Production Readiness** — Env config separated, zero secrets in code, zero-downtime deploy

## Feature Scope
Fullstack — Frontend + Backend + API

## Success Criteria
Follow Phase 3 acceptance criteria from phases.md as-is:
- Sprint created -> items moved -> capacity indicator correct
- Sprint started -> scope locked -> addition needs confirmation
- Burndown data written daily at 11:59 PM for active sprints
- Overdue release detected within 24h, PO + TL notified
- Stale item warning appears after 14 days no update
- Zero P0/P1 bugs at release gate
- Application layer coverage >=70%
- Backlog 1000 items loads + filters <500ms
- No endpoint >1 second under normal load
- 1 week dogfooding — zero crashes, zero data loss
- Logs sufficient to debug any production bug without local reproduction
- Production deploy zero downtime
- Health check correctly reports degraded when Postgres or RabbitMQ down
- Lighthouse score >=80 on main screens

## Constraints
- Non-negotiable: TFD, sealed classes, Result pattern, CQRS, IPermissionChecker
- WorkItemHistories append-only
- No feature creep — new requests go to backlog
- All migrations backward compatible
- API contracts don't change without version bump
