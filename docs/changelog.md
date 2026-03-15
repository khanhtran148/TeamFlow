# Changelog

## [Unreleased]

### Phase 1 — Work Item Management (2026-03-15)

- Project CRUD (create, update, archive, delete, list with filter/search)
- Work item CRUD with hierarchy (Epic > Story > Task / Bug / Spike)
- Parent-child type enforcement and soft-delete cascade to children
- Work item status transitions with validation
- Single-assignee assignment with history tracking via append-only `WorkItemHistories`
- Item linking with 6 link types, bidirectional storage, circular blocking detection
- Release CRUD, item assignment with one-release-per-item constraint
- Backlog query: hierarchy-grouped, filtered by status/priority/assignee/type/sprint/release, searchable, reorderable
- Kanban board query: status-grouped columns, swimlanes by assignee or epic
- Realtime broadcast infrastructure: domain events published via MassTransit to RabbitMQ, consumed by background service, broadcast via SignalR hub
- Domain event persistence to event store table
- 124 tests: 17 domain, 99 application, 8 integration

### Phase 0 — Foundation (2026-03-15)

- .NET 10 solution (`TeamFlow.slnx`) with Clean Architecture + Vertical Slice layout
- 21 domain entities, 10 enums, 16 domain events across `TeamFlow.Domain`
- EF Core `TeamFlowDbContext` with 21 `DbSet<T>` properties and global soft-delete query filter (`deleted_at IS NULL`)
- Docker Compose: PostgreSQL 17, RabbitMQ 4 with management UI; health-checked dependency ordering
- API skeleton: versioning (`/api/v{version}/`), rate limiting (5 policies), health checks, Swagger/OpenAPI
- `ApiControllerBase` with `HandleResult<T>` mapping `Result<T>` errors to `ProblemDetails` responses
- SignalR hub registered and routed
- MassTransit configured against RabbitMQ with dead-letter queue support
- Quartz.NET scheduler wired into `TeamFlow.BackgroundServices`
- Test infrastructure: `IntegrationTestBase` with Testcontainers (real PostgreSQL), test data builders pattern established
