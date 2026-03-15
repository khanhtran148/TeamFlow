# 08 — Definition of Done, Rules & Risk Register

## Definition of Done

A feature is **Done** only when **all** of the following are true:

### Code Quality
- [ ] Unit test: happy path + at least one edge case per MediatR handler
- [ ] API returns `ProblemDetails` (RFC 7807) for all invalid inputs
- [ ] No `500` response on valid, well-formed input
- [ ] No hard-coded secrets, connection strings, or environment values in committed code

### Security
- [ ] Rate limiting applied with correct named policy for endpoint type
- [ ] Permission check enforced — unauthorized callers receive `403 Forbidden`
- [ ] No breaking API contract change without version bump (`/api/v2/...`)

### Process
- [ ] PR reviewed and approved by ≥1 developer before merge
- [ ] Feature runs without error on shared Dev environment with seed data

### Observability
- [ ] Correlation ID present in all log entries for the feature
- [ ] Realtime events publishing correctly — verified in RabbitMQ management UI
- [ ] SignalR broadcast confirmed reaching connected clients

### Data Integrity
- [ ] History records written correctly for all mutations — verified by integration test
- [ ] No UPDATE or DELETE against WorkItemHistories for any code path in this feature

---

## Cross-Phase Rules

1. **No feature creep during a phase** — any new request goes to backlog for the next phase. No exceptions.

2. **Bugs first** — bugs from previous phases take priority over new feature work in the current phase.

3. **Demo gate** — each phase ends with a team demo. All acceptance criteria confirmed before moving on. No "we'll fix it later."

4. **Backward-compatible migrations** — never DROP a column in the same deployment as the code change that removes its reference. Always use a two-phase deploy:
   - Deploy 1: code ignores column, migration adds nullable column
   - Deploy 2: code uses new column
   - Deploy 3: drop old column

5. **API contract discipline** — contracts do not change without:
   - Version bump on the affected endpoint group
   - Written agreement from both Frontend and Backend leads
   - Updated OpenAPI spec

6. **Zero secrets in source control** — no exceptions, no "temporary", no "dev only" excuses.

7. **REST fallback for realtime** — every realtime feature must work without SignalR. SignalR is a UX enhancement, not a correctness dependency.

8. **Append-only history** — no UPDATE or DELETE statements are ever written against `work_item_histories`. Enforced at the repository level.

9. **Claude Code review rule** — no Claude Code output merges without human review. Complex logic (permission resolution, circular detection, auth flow) requires extra scrutiny.

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Permission system complexity delays Phase 2 | High | High | Build Org + Team levels first. Individual override as separate sub-task. Do not merge until first two levels stable and tested. |
| Team unfamiliar with Vertical Slice — merge conflicts early | Medium | Medium | Workshop Week 1. Pair programming Weeks 1–2. Senior review all slice PRs initially. |
| SignalR scaling under production load | Medium | Medium | Add Redis backplane from Phase 0 if planning multiple API replicas — cheaper early than retrofit. |
| Circular link detection edge cases under concurrent requests | Medium | High | Database-level locking or optimistic concurrency on link creation. Load test in Phase 3. |
| Schema changes mid-phase break migrations | Medium | High | All migrations backward compatible. Two-phase deploy for column removals. |
| Frontend/Backend API contract drift | Medium | Medium | Contract locked at Phase 0. Any change requires both leads sign-off + version bump. OpenAPI as enforcement. |
| RabbitMQ more complex than estimated | Low | Medium | MassTransit abstraction allows in-process queue fallback during development. Production always RabbitMQ. |
| Retro anonymous mode leaks identity | Low | Medium | `author_id` stored for moderation but never returned in API response when anonymous mode. Enforced at query projection, not post-fetch filter. |
| Claude Code generates incorrect permission logic | Medium | High | Permission resolution handler: human writes, not delegated to Claude Code. Claude Code can scaffold the structure but human writes the resolution logic. |
| EventPartitionCreatorJob missed → insert failures | Low | Critical | Misfire policy: FireNow. Alert on failure. Manual partition creation runbook documented. |

---

## Claude Code — What to Delegate, What Not To

### Safe to delegate fully
- CRUD handler scaffold (command, handler, validator, response DTO)
- Controller methods — route mapping, result handling
- EF Core entity configuration (Fluent API)
- Migration generation from entity changes
- Unit test scaffolding for handlers
- Integration test boilerplate (Testcontainers setup, test data builders)
- Email templates
- Dashboard query implementations (once schema confirmed)
- Background job boilerplate (IHostedService, Quartz job shell)
- SignalR Hub connection events

### Delegate with careful review
- Permission resolution logic — review every branch
- Authentication flow — review token generation and validation
- Circular dependency detection in item linking
- Retro anonymity — verify author_id never leaks in API responses
- Migration scripts — especially anything involving existing data
- RabbitMQ/MassTransit consumer configuration

### Do not delegate — human writes
- Permission resolution algorithm (Individual → Team → Org)
- Security-sensitive code (JWT validation, password hashing)
- Data archival job (irreversible operations)
- CLAUDE.md architecture decisions
- Database partition strategy

---

## CLAUDE.md Maintenance Rules

- Update CLAUDE.md when a new pattern is established
- Update CLAUDE.md when a convention changes
- Every PR that introduces a new architectural pattern must update CLAUDE.md
- CLAUDE.md is reviewed at the start of each phase

See [CLAUDE.md](./CLAUDE.md) for current instructions.
