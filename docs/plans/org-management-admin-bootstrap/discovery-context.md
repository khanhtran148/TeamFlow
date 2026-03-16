# Discovery Context: Org Management & Admin Bootstrap

**Date:** 2026-03-16
**ADR:** `docs/adrs/260316-org-management-admin-bootstrap.md`
**Brainstorm:** `docs/brainstorms/org-management-and-admin-bootstrap-20260316/discovery-context.md`

## Problem Statement

TeamFlow has no organization management or system admin capabilities. Users register but cannot create or join orgs. No admin account exists on first launch. The frontend uses a hardcoded org ID. The permission system has a bootstrap hack in `PermissionChecker.IsOrgAdminAsync` that grants implicit admin when no OrgAdmin membership exists.

## Current State (from Scout)

### Domain Layer
- `User` entity: `Email`, `PasswordHash`, `Name` -- no system role
- `Organization` entity: `Id`, `Name`, `CreatedByUserId`, `CreatedAt` -- not BaseEntity, no slug
- `ProjectMembership`: `ProjectRole.OrgAdmin` used as implicit org admin
- No `OrganizationMember` entity
- No `Invitation` entity

### Infrastructure Layer
- `PermissionChecker.IsOrgAdminAsync()`: Bootstrap hack at line 121-128 -- allows any user when no OrgAdmin exists
- `OrganizationRepository.ListByUserAsync()`: Derives org membership from creator or project membership
- `OrganizationConfiguration`: Minimal -- no slug column, no `CreatedByUserId` mapping
- `AuthService.GenerateJwt()`: No system role claim emitted

### API Layer
- `OrganizationsController`: Create (no permission check), GetById (direct repo call, violates pattern), List
- `Program.cs` line 202-206: Seeds "Default Organization" if none exists -- no admin seed
- No admin endpoints, no invitation endpoints

### Frontend
- Auth store: `AuthUser { id, email, name }` -- no system role
- `JwtCurrentUser`: No system role parsed from JWT
- Root page redirects to `/projects` -- no org context in URL
- No org switcher, no onboarding flow
- Middleware passes through everything -- no org-aware routing

### Test Infrastructure
- `IntegrationTestBase`: Seeds `SeedOrgId` and `SeedUserId`, uses `AlwaysAllowTestPermissionChecker`
- `UserBuilder`: No system role support
- `OrganizationBuilder`: Exists but basic

## Key Decisions (from ADR)

| ID | Decision | Choice |
|----|----------|--------|
| D1 | SystemRole storage | `SystemRole` enum on `User` entity |
| D2 | Org membership | Dedicated `OrganizationMember` table with `OrgRole` |
| D3 | Invitation tokens | Database-backed opaque tokens, 7-day expiry |
| D4 | Frontend org context | URL-based `/org/{slug}/...` routing |
| D5 | Admin dashboard | Separate `/admin` route |

## New Domain Types

```
enum SystemRole { User, SystemAdmin }
enum OrgRole { Owner, Admin, Member }
enum InviteStatus { Pending, Accepted, Expired, Revoked }

sealed class OrganizationMember : BaseEntity
  - OrganizationId (Guid, FK)
  - UserId (Guid, FK)
  - Role (OrgRole)
  - JoinedAt (DateTime)

sealed class Invitation : BaseEntity
  - OrganizationId (Guid, FK)
  - InvitedByUserId (Guid, FK)
  - Email (string, nullable for email-less MVP)
  - Role (OrgRole)
  - TokenHash (string)
  - Status (InviteStatus)
  - ExpiresAt (DateTime)
  - AcceptedAt (DateTime?)
  - AcceptedByUserId (Guid?)

Modified: User (add SystemRole property)
Modified: Organization (add Slug property, make BaseEntity)
```

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Users lose access during migration | High | High | Backfill OrganizationMember from ProjectMembership OrgAdmin records |
| Seeded admin gets deleted | Low | Critical | Prevent last SystemAdmin deletion + config re-seed on startup |
| Frontend route regressions | Medium | Medium | Redirect middleware for old URLs + E2E tests |
| Invitation token leaked | Medium | Medium | SHA-256 hashed storage, 7-day expiry, one-time use |

## Files That Will Change

### New Files (per phase -- see plan.md for full list)
- Domain: `SystemRole.cs`, `OrgRole.cs`, `InviteStatus.cs`, `OrganizationMember.cs`, `Invitation.cs`
- Application: ~30 new handler/validator/DTO files across 6 feature slices
- Infrastructure: 2 new configurations, 2 new repositories, 1 migration, 1 hosted service
- API: 2 new controllers, 1 authorization attribute
- Frontend: ~15 new components/pages, route restructure

### Modified Files
- `User.cs`, `Organization.cs` (domain changes)
- `UserConfiguration.cs`, `OrganizationConfiguration.cs` (EF config)
- `TeamFlowDbContext.cs` (new DbSets)
- `PermissionChecker.cs` (remove bootstrap hack, add org membership check)
- `AuthService.cs` (emit SystemRole claim in JWT)
- `JwtCurrentUser.cs` (parse SystemRole from JWT)
- `ICurrentUser.cs` (add SystemRole property)
- `Program.cs` (replace org seed with admin seed service)
- `DependencyInjection.cs` (register new repos/services)
- `appsettings.json` (add SystemAdmin seed config)
- Frontend: `auth-store.ts`, `auth-guard.tsx`, `middleware.ts`, `layout.tsx`, all route files
