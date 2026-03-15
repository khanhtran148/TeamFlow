# API Contract — Integration & E2E Testing Infrastructure (Phase 1)

**Version:** 1.0.0
**Date:** 2026-03-15
**Status:** FINAL

---

## Scope

Phase 1 does NOT introduce new API endpoints. It builds backend integration test infrastructure
(PostgresFixture, IntegrationTestWebAppFactory, ApiIntegrationTestBase, Respawn) that exercises
**existing** API endpoints via HTTP-level integration tests.

## Existing Endpoints Exercised (Proof of Concept)

### Projects API

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/v1/projects` | Bearer JWT | Create a project |
| GET | `/api/v1/projects/{id}` | Bearer JWT | Get project by ID |
| GET | `/api/v1/projects` | Bearer JWT | List projects (paginated) |
| PUT | `/api/v1/projects/{id}` | Bearer JWT | Update project |
| POST | `/api/v1/projects/{id}/archive` | Bearer JWT | Archive project |
| DELETE | `/api/v1/projects/{id}` | Bearer JWT | Delete project |

### Health

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/health` | None | Health check endpoint |

## Test Infrastructure Components

| Component | File | Purpose |
|-----------|------|---------|
| PostgresFixture | `tests/TeamFlow.Api.Tests/Infrastructure/PostgresFixture.cs` | Shared Testcontainer PostgreSQL instance |
| IntegrationTestWebAppFactory | `tests/TeamFlow.Api.Tests/Infrastructure/IntegrationTestWebAppFactory.cs` | WebApplicationFactory with test overrides |
| ApiIntegrationTestBase | `tests/TeamFlow.Api.Tests/Infrastructure/ApiIntegrationTestBase.cs` | Base class with Respawn, JWT helper, seeding |

## JWT Configuration (Test)

- Issuer: `TeamFlow`
- Audience: `TeamFlow.Users`
- Secret: 64-character test-only secret
- Claims: `sub` (user ID), `email`, `name`, `jti`

## Shared Types

No new shared types introduced. Tests consume existing DTOs:
- `ProjectDto` from `TeamFlow.Application.Features.Projects`

## TBD / Pending

None for Phase 1.
