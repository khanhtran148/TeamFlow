# Plan: Org Management & Admin Bootstrap

**Created:** 2026-03-16
**Feature:** Organization lifecycle, admin bootstrap, multi-org support
**Type:** Fullstack (Backend + Frontend + API)
**ADR:** `docs/adrs/260316-org-management-admin-bootstrap.md`
**Discovery:** `docs/plans/org-management-admin-bootstrap/discovery-context.md`

---

## Overview

Replace TeamFlow's implicit bootstrap hack and hardcoded org ID with proper system admin seeding, org-level membership, invitation-based onboarding, and URL-scoped multi-org frontend.

6 phases, executed sequentially. Phases 4 and 5 (Frontend Multi-Org UX and Onboarding) run in parallel after Phase 3 is complete. Each phase has a clear stopping point. TFD mandatory on all backend work.

## Phase Dependencies

```
Phase 1 (Admin Bootstrap)
    |
Phase 2 (Org Membership Model)
    |
Phase 3 (Invitation System)
    |
    +--- Phase 4 (Frontend Multi-Org UX) ---+
    |                                        |
    +--- Phase 5 (Onboarding Flow) ---------+
    |                                        |
    +----------------------------------------+
    |
Phase 6 (Member Management)
```

---

## API Contract

**File:** `docs/plans/org-management-admin-bootstrap/api-contract-260316-1500.md`

See separate contract file for full endpoint definitions. Summary:

| Endpoint | Method | Auth | Phase |
|----------|--------|------|-------|
| `POST /api/v1/admin/seed` | POST | Internal only | 1 |
| `GET /api/v1/admin/organizations` | GET | SystemAdmin | 1 |
| `GET /api/v1/admin/users` | GET | SystemAdmin | 1 |
| `POST /api/v1/organizations` | POST | SystemAdmin | 2 |
| `PUT /api/v1/organizations/{id}` | PUT | Org Owner/Admin | 2 |
| `GET /api/v1/organizations/{slug}` | GET | Org Member | 2 |
| `GET /api/v1/me/organizations` | GET | Authenticated | 2 |
| `POST /api/v1/organizations/{orgId}/invitations` | POST | Org Admin/Owner | 3 |
| `GET /api/v1/organizations/{orgId}/invitations` | GET | Org Admin/Owner | 3 |
| `POST /api/v1/invitations/{token}/accept` | POST | Authenticated | 3 |
| `DELETE /api/v1/invitations/{id}` | DELETE | Org Admin/Owner | 3 |
| `GET /api/v1/organizations/{orgId}/members` | GET | Org Member | 6 |
| `PUT /api/v1/organizations/{orgId}/members/{userId}/role` | PUT | Org Owner/Admin | 6 |
| `DELETE /api/v1/organizations/{orgId}/members/{userId}` | DELETE | Org Owner/Admin | 6 |

---

## Phase 1: System Admin Bootstrap — **completed**

**Goal:** Seed a SystemAdmin user from config on first startup. Add system-level authorization.
**Dependencies:** None
**Estimated effort:** M

### 1.1 Domain Changes [S]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Domain.Tests/EnumTests.cs` | Add `SystemRole` enum value coverage |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Domain/Enums/SystemRole.cs` | CREATE | `enum SystemRole { User, SystemAdmin }` |
| `src/core/TeamFlow.Domain/Entities/User.cs` | MODIFY | Add `public SystemRole SystemRole { get; set; } = SystemRole.User;` |

### 1.2 Infrastructure Changes [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Infrastructure.Tests/Services/AdminSeedServiceTests.cs` | CREATE: Seed creates admin when not exists; Seed skips when admin exists (idempotent); Seed fails gracefully on missing config |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | MODIFY | Add `SystemRole` column mapping (`system_role`, int, default 0) |
| `src/core/TeamFlow.Infrastructure/Services/AdminSeedService.cs` | CREATE | `IHostedService` that reads `SystemAdmin:Email` and `SystemAdmin:Password` from config, creates user if not exists, sets `SystemRole = SystemAdmin` |
| `src/core/TeamFlow.Infrastructure/Services/AuthService.cs` | MODIFY | Add `system_role` claim to JWT payload |
| `src/apps/TeamFlow.Api/appsettings.json` | MODIFY | Add `"SystemAdmin": { "Email": "", "Password": "" }` section |

### 1.3 Application Layer [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Admin/ListAdminOrganizationsTests.cs` | CREATE: Returns all orgs for SystemAdmin; Rejects non-SystemAdmin |
| `tests/TeamFlow.Application.Tests/Features/Admin/ListAdminUsersTests.cs` | CREATE: Returns all users for SystemAdmin; Rejects non-SystemAdmin |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Common/Interfaces/ICurrentUser.cs` | MODIFY | Add `SystemRole SystemRole { get; }` |
| `src/core/TeamFlow.Application/Common/Interfaces/IUserRepository.cs` | MODIFY | Add `Task<IEnumerable<User>> ListAllAsync(CancellationToken ct)` |
| `src/core/TeamFlow.Application/Features/Admin/ListOrganizations/AdminListOrganizationsQuery.cs` | CREATE | Query record |
| `src/core/TeamFlow.Application/Features/Admin/ListOrganizations/AdminListOrganizationsHandler.cs` | CREATE | Check SystemRole, return all orgs |
| `src/core/TeamFlow.Application/Features/Admin/ListUsers/AdminListUsersQuery.cs` | CREATE | Query record |
| `src/core/TeamFlow.Application/Features/Admin/ListUsers/AdminListUsersHandler.cs` | CREATE | Check SystemRole, return all users |
| `src/core/TeamFlow.Application/Features/Admin/AdminUserDto.cs` | CREATE | DTO for admin user list |

### 1.4 API Layer [S]

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Services/JwtCurrentUser.cs` | MODIFY | Parse `system_role` claim |
| `src/apps/TeamFlow.Api/Controllers/AdminController.cs` | CREATE | `GET /admin/organizations`, `GET /admin/users` with SystemAdmin check |
| `src/apps/TeamFlow.Api/Program.cs` | MODIFY | Register `AdminSeedService` as hosted service; Remove "Default Organization" seed |

### 1.5 Test Infrastructure Updates [S]

| File | Action | Description |
|------|--------|-------------|
| `tests/TeamFlow.Tests.Common/TestStubs.cs` | MODIFY | Add `SystemRole` to `TestCurrentUser` |
| `tests/TeamFlow.Tests.Common/Builders/UserBuilder.cs` | MODIFY | Add `WithSystemRole(SystemRole)` builder method |

### 1.6 EF Migration [S]

| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Migrations/YYYYMMDD_AddSystemRole.cs` | CREATE | Add `system_role` column to `users` table, default `0` (User) |

**Phase 1 exit criteria:** `dotnet test` passes. Admin seeded from config on startup. Admin endpoints return data. Non-admins get 403.

- [x] `dotnet test` passes (802 tests, 0 failures)
- [x] `SystemRole` enum created with `User` and `SystemAdmin` values
- [x] `User` entity updated with `SystemRole` property
- [x] `AdminSeedService` created (idempotent, uses IServiceScopeFactory)
- [x] `system_role` JWT claim added to `AuthService`
- [x] `ICurrentUser` interface updated with `SystemRole` property
- [x] `AdminListOrganizationsHandler` and `AdminListUsersHandler` created
- [x] `AdminController` created with GET /admin/organizations and GET /admin/users
- [x] `JwtCurrentUser` parses `system_role` claim
- [x] `AdminSeedService` registered in Program.cs
- [x] Default org seed removed from Program.cs
- [x] EF migration `AddSystemRole` generated
- [x] `TestCurrentUser` stub updated with `SystemRole`
- [x] `UserBuilder` updated with `WithSystemRole` method
- [x] Admin frontend: guard, layout, dashboard, orgs page, users page created

---

## Phase 2: Org Membership Model — **completed**

**Goal:** Proper org-level membership with OrganizationMember entity. Refactor PermissionChecker.
**Dependencies:** Phase 1 complete
**Estimated effort:** L

### 2.1 Domain Changes [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Domain.Tests/EnumTests.cs` | MODIFY: Add `OrgRole` enum coverage |
| `tests/TeamFlow.Domain.Tests/Entities/OrganizationTests.cs` | CREATE: Slug generation, validation |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Domain/Enums/OrgRole.cs` | CREATE | `enum OrgRole { Owner, Admin, Member }` |
| `src/core/TeamFlow.Domain/Entities/OrganizationMember.cs` | CREATE | `sealed class` with `OrganizationId`, `UserId`, `Role (OrgRole)`, `JoinedAt` |
| `src/core/TeamFlow.Domain/Entities/Organization.cs` | MODIFY | Inherit from `BaseEntity`; Add `Slug` property; Add `ICollection<OrganizationMember> Members` nav |

### 2.2 Infrastructure Changes [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Infrastructure.Tests/Repositories/OrganizationMemberRepositoryTests.cs` | CREATE: Add/get/list members; Check membership existence |
| `tests/TeamFlow.Infrastructure.Tests/Services/PermissionCheckerTests.cs` | MODIFY: Remove bootstrap hack tests; Add org membership resolution tests |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs` | MODIFY | Add `slug` column (unique index), `created_by_user_id` mapping, `updated_at` |
| `src/core/TeamFlow.Infrastructure/Persistence/Configurations/OrganizationMemberConfiguration.cs` | CREATE | Table `organization_members`, composite unique index on `(organization_id, user_id)` |
| `src/core/TeamFlow.Infrastructure/Persistence/TeamFlowDbContext.cs` | MODIFY | Add `DbSet<OrganizationMember> OrganizationMembers` |
| `src/core/TeamFlow.Infrastructure/Repositories/OrganizationMemberRepository.cs` | CREATE | CRUD operations for org membership |
| `src/core/TeamFlow.Infrastructure/Repositories/OrganizationRepository.cs` | MODIFY | Add `GetBySlugAsync`, `ListByMembershipAsync` (replace `ListByUserAsync`) |
| `src/core/TeamFlow.Infrastructure/Services/PermissionChecker.cs` | MODIFY | Replace bootstrap hack with `OrganizationMember` lookup; Keep Individual -> Team -> Org resolution |
| `src/core/TeamFlow.Infrastructure/DependencyInjection.cs` | MODIFY | Register `IOrganizationMemberRepository` |

### 2.3 Application Layer [L]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Organizations/CreateOrganizationTests.cs` | MODIFY: Require SystemAdmin role; Auto-create Owner membership for creator |
| `tests/TeamFlow.Application.Tests/Features/Organizations/UpdateOrganizationTests.cs` | CREATE: Owner/Admin can update; Member cannot; Not-found returns error |
| `tests/TeamFlow.Application.Tests/Features/Organizations/GetOrganizationBySlugTests.cs` | CREATE: Returns org for member; Returns not-found for non-member |
| `tests/TeamFlow.Application.Tests/Features/Organizations/ListMyOrganizationsTests.cs` | CREATE: Returns orgs where user is member |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Common/Interfaces/IOrganizationRepository.cs` | MODIFY | Add `GetBySlugAsync`, `ExistsBySlugAsync` |
| `src/core/TeamFlow.Application/Common/Interfaces/IOrganizationMemberRepository.cs` | CREATE | Interface for org member operations |
| `src/core/TeamFlow.Application/Features/Organizations/CreateOrganization/CreateOrganizationHandler.cs` | MODIFY | Add SystemAdmin check; Generate slug; Create OrganizationMember(Owner) for creator |
| `src/core/TeamFlow.Application/Features/Organizations/CreateOrganization/CreateOrganizationValidator.cs` | MODIFY | Add slug uniqueness check |
| `src/core/TeamFlow.Application/Features/Organizations/UpdateOrganization/UpdateOrganizationCommand.cs` | CREATE | Command with `OrgId`, `Name`, `Slug` |
| `src/core/TeamFlow.Application/Features/Organizations/UpdateOrganization/UpdateOrganizationHandler.cs` | CREATE | Org Owner/Admin permission check |
| `src/core/TeamFlow.Application/Features/Organizations/UpdateOrganization/UpdateOrganizationValidator.cs` | CREATE | Validate name and slug |
| `src/core/TeamFlow.Application/Features/Organizations/GetBySlug/GetOrganizationBySlugQuery.cs` | CREATE | Query by slug |
| `src/core/TeamFlow.Application/Features/Organizations/GetBySlug/GetOrganizationBySlugHandler.cs` | CREATE | Check org membership |
| `src/core/TeamFlow.Application/Features/Organizations/ListMyOrganizations/ListMyOrganizationsQuery.cs` | CREATE | Query for current user's orgs |
| `src/core/TeamFlow.Application/Features/Organizations/ListMyOrganizations/ListMyOrganizationsHandler.cs` | CREATE | Return orgs via OrganizationMember |
| `src/core/TeamFlow.Application/Features/Organizations/OrganizationDto.cs` | MODIFY | Add `Slug` property |

### 2.4 API Layer [S]

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/OrganizationsController.cs` | MODIFY | Add PUT update, GET by-slug, GET me/organizations endpoints; Fix GetById to use handler |

### 2.5 Data Migration [M]

| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Migrations/YYYYMMDD_AddOrgMembershipModel.cs` | CREATE | Add `slug` + `updated_at` to organizations; Create `organization_members` table; Backfill: for each user with `ProjectRole.OrgAdmin`, create `OrganizationMember(Owner)`; Generate slug from org name |

### 2.6 Test Infrastructure [S]

| File | Action | Description |
|------|--------|-------------|
| `tests/TeamFlow.Tests.Common/Builders/OrganizationBuilder.cs` | MODIFY | Add `WithSlug()` |
| `tests/TeamFlow.Tests.Common/Builders/OrganizationMemberBuilder.cs` | CREATE | Builder for OrganizationMember |
| `tests/TeamFlow.Tests.Common/TestDataSeeder.cs` | MODIFY | Seed OrganizationMember for test user |
| `tests/TeamFlow.Tests.Common/IntegrationTestBase.cs` | MODIFY | Register IOrganizationMemberRepository |

**Phase 2 exit criteria:** `dotnet test` passes. Only SystemAdmin can create orgs. Org membership stored in `OrganizationMember`. PermissionChecker uses org membership (no bootstrap hack). Existing data migrated.

- [x] `dotnet test` passes (870 tests, 0 failures)
- [x] `OrgRole` enum created with Owner, Admin, Member values
- [x] `OrganizationMember` sealed entity created
- [x] `Organization` entity updated: inherits BaseEntity, Slug property, Members navigation, GenerateSlug() static method
- [x] `OrganizationConfiguration` updated with slug (unique), updated_at, created_by_user_id
- [x] `OrganizationMemberConfiguration` created with composite unique index
- [x] `TeamFlowDbContext.OrganizationMembers` DbSet added
- [x] `OrganizationMemberRepository` created with CRUD operations
- [x] `OrganizationRepository` updated with GetBySlugAsync, ExistsBySlugAsync, UpdateAsync, ListByUserAsync uses OrganizationMember
- [x] `PermissionChecker` bootstrap hack removed — IsOrgAdminAsync checks OrganizationMembers table
- [x] `IOrganizationMemberRepository` interface created
- [x] `IOrganizationRepository` interface updated with GetBySlugAsync, ExistsBySlugAsync, UpdateAsync
- [x] `IOrganizationMemberRepository` registered in DependencyInjection.cs
- [x] `CreateOrganizationCommand` updated with Slug optional parameter
- [x] `CreateOrganizationHandler` adds SystemAdmin check, slug generation, Owner membership creation
- [x] `CreateOrganizationValidator` adds slug uniqueness validation
- [x] `OrganizationDto` updated with Slug property
- [x] `UpdateOrganizationCommand/Handler/Validator` created
- [x] `GetOrganizationBySlugQuery/Handler` created with membership check
- [x] `ListMyOrganizationsQuery/Handler` created
- [x] `OrganizationsController` updated: PUT update, GET by-slug, new MeController for GET me/organizations
- [x] EF migration `AddOrgMembershipModel` generated with data backfill SQL
- [x] `OrganizationBuilder` updated with WithSlug()
- [x] `OrganizationMemberBuilder` created
- [x] `TestDataSeeder` updated (org with slug, no auto-membership)
- [x] `IntegrationTestBase` registers IOrganizationMemberRepository
- [x] `ApiIntegrationTestBase` updated with SeedOrgMemberAsync helper; SeedProjectAsync uses OrganizationMember for admin anchor

---

## Phase 3: Invitation System

**Goal:** Database-backed invite tokens for org onboarding.
**Dependencies:** Phase 2 complete
**Estimated effort:** L

### 3.1 Domain Changes [S]

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Domain.Tests/EnumTests.cs` | MODIFY: Add `InviteStatus` enum coverage |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Domain/Enums/InviteStatus.cs` | CREATE | `enum InviteStatus { Pending, Accepted, Expired, Revoked }` |
| `src/core/TeamFlow.Domain/Entities/Invitation.cs` | CREATE | Sealed class with all fields from discovery context |

### 3.2 Infrastructure Changes [M]

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Infrastructure.Tests/Repositories/InvitationRepositoryTests.cs` | CREATE: Add, get by token hash, list by org, update status |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Persistence/Configurations/InvitationConfiguration.cs` | CREATE | Table `invitations`, index on `token_hash` |
| `src/core/TeamFlow.Infrastructure/Persistence/TeamFlowDbContext.cs` | MODIFY | Add `DbSet<Invitation>` |
| `src/core/TeamFlow.Infrastructure/Repositories/InvitationRepository.cs` | CREATE | CRUD + GetByTokenHashAsync |
| `src/core/TeamFlow.Infrastructure/DependencyInjection.cs` | MODIFY | Register IInvitationRepository |

### 3.3 Application Layer [L]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Invitations/CreateInvitationTests.cs` | CREATE: Org Admin/Owner can create; Member cannot; Generates token; Sets 7-day expiry |
| `tests/TeamFlow.Application.Tests/Features/Invitations/AcceptInvitationTests.cs` | CREATE: Valid token creates membership; Expired token rejected; Already-accepted token rejected; Revoked token rejected |
| `tests/TeamFlow.Application.Tests/Features/Invitations/RevokeInvitationTests.cs` | CREATE: Org Admin/Owner can revoke; Already-accepted cannot be revoked |
| `tests/TeamFlow.Application.Tests/Features/Invitations/ListInvitationsTests.cs` | CREATE: Returns org invitations for Admin/Owner |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Common/Interfaces/IInvitationRepository.cs` | CREATE | Interface |
| `src/core/TeamFlow.Application/Features/Invitations/InvitationDto.cs` | CREATE | DTO (no raw token in response) |
| `src/core/TeamFlow.Application/Features/Invitations/Create/CreateInvitationCommand.cs` | CREATE | OrgId, Email (optional), Role |
| `src/core/TeamFlow.Application/Features/Invitations/Create/CreateInvitationHandler.cs` | CREATE | Permission check, generate token, hash with SHA-256, persist |
| `src/core/TeamFlow.Application/Features/Invitations/Create/CreateInvitationValidator.cs` | CREATE | Validate role enum, optional email format |
| `src/core/TeamFlow.Application/Features/Invitations/Accept/AcceptInvitationCommand.cs` | CREATE | Raw token string |
| `src/core/TeamFlow.Application/Features/Invitations/Accept/AcceptInvitationHandler.cs` | CREATE | Hash token, look up, check status+expiry, create OrganizationMember, update status |
| `src/core/TeamFlow.Application/Features/Invitations/Revoke/RevokeInvitationCommand.cs` | CREATE | InvitationId |
| `src/core/TeamFlow.Application/Features/Invitations/Revoke/RevokeInvitationHandler.cs` | CREATE | Permission check, update status |
| `src/core/TeamFlow.Application/Features/Invitations/List/ListInvitationsQuery.cs` | CREATE | OrgId |
| `src/core/TeamFlow.Application/Features/Invitations/List/ListInvitationsHandler.cs` | CREATE | Permission check, return filtered list |

### 3.4 API Layer [S]

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/InvitationsController.cs` | CREATE | POST create, POST accept, DELETE revoke, GET list |

### 3.5 EF Migration [S]

| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Infrastructure/Migrations/YYYYMMDD_AddInvitations.cs` | CREATE | Create `invitations` table |

### 3.6 Test Infrastructure [S]

| File | Action | Description |
|------|--------|-------------|
| `tests/TeamFlow.Tests.Common/Builders/InvitationBuilder.cs` | CREATE | Builder for Invitation entity |

**Phase 3 exit criteria:** `dotnet test` passes. Invitations can be created, accepted, revoked. Token is hashed in DB. Expired tokens rejected. Accepting creates OrganizationMember.

- [x] `dotnet test` passes (916 tests, 0 failures)
- [x] `InviteStatus` enum created with Pending, Accepted, Expired, Revoked values
- [x] `Invitation` sealed entity created with all required fields + navigations
- [x] `InvitationConfiguration` created: table `invitations`, unique index on `token_hash`, FK constraints
- [x] `TeamFlowDbContext.Invitations` DbSet added
- [x] `IInvitationRepository` interface created
- [x] `InvitationRepository` created with Add, GetByTokenHash, GetById, ListByOrg, Update
- [x] `IInvitationRepository` registered in DependencyInjection.cs and IntegrationTestBase
- [x] `InvitationDto`, `CreateInvitationResponse`, `AcceptInvitationResponse` DTOs created
- [x] `CreateInvitationCommand/Handler/Validator` created (permission check, token gen, SHA-256 hash, 7-day expiry)
- [x] `AcceptInvitationCommand/Handler` created (hash token, check status+expiry, create OrganizationMember)
- [x] `RevokeInvitationCommand/Handler` created (permission check, cannot revoke accepted)
- [x] `ListInvitationsQuery/Handler` created (permission check, returns mapped DTOs)
- [x] `InvitationsController` created: POST create, GET list, POST accept, DELETE revoke
- [x] EF migration `AddInvitations` generated
- [x] `InvitationBuilder` created in test infrastructure
- [x] `InvitationRepositoryTests` pass (7 tests)
- [x] `CreateInvitationTests` pass (12 tests)
- [x] `AcceptInvitationTests` pass (8 tests)
- [x] `RevokeInvitationTests` pass (7 tests)
- [x] `ListInvitationsTests` pass (4 tests)
- [x] `EnumTests` updated with InviteStatus coverage (5 new tests)

---

## Phase 4: Frontend Multi-Org UX — **completed**

**Goal:** URL-based org routing, org switcher, remove hardcoded org ID.
**Dependencies:** Phase 3 complete
**PARALLEL: yes (with Phase 5)**
**Estimated effort:** L

### FILE OWNERSHIP
This phase owns ALL files under `src/apps/teamflow-web/`. No other phase modifies frontend files while this runs.

### 4.1 API Client & Types [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/lib/api/types.ts` | MODIFY | Add `OrganizationDto`, `OrgRole`, `InvitationDto`, `OrganizationMemberDto` types; Add `systemRole` to `AuthUser` |
| `src/apps/teamflow-web/lib/api/organizations.ts` | CREATE | API functions: `listMyOrgs()`, `getOrgBySlug(slug)`, `createOrg()`, `updateOrg()` |
| `src/apps/teamflow-web/lib/api/invitations.ts` | CREATE | API functions: `createInvitation()`, `acceptInvitation(token)`, `revokeInvitation()`, `listInvitations()` |
| `src/apps/teamflow-web/lib/api/members.ts` | CREATE | API functions for Phase 6 (stub for now) |

### 4.2 Auth & State Updates [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/lib/stores/auth-store.ts` | MODIFY | Add `systemRole` to `AuthUser`; Parse from JWT |
| `src/apps/teamflow-web/lib/hooks/use-organizations.ts` | CREATE | TanStack Query hooks: `useMyOrganizations()`, `useOrganizationBySlug(slug)` |
| `src/apps/teamflow-web/lib/stores/org-store.ts` | CREATE | Zustand store for current org context (derived from URL slug) |
| `src/apps/teamflow-web/lib/contexts/org-context.tsx` | CREATE | React context that reads slug from URL params and provides current org |

### 4.3 Route Restructure [L]

Move all project/team routes under `/org/[slug]/...`:

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/org/[slug]/layout.tsx` | CREATE | Org-scoped layout, fetches org by slug, provides OrgContext |
| `src/apps/teamflow-web/app/org/[slug]/projects/page.tsx` | CREATE | Projects list (move from `app/projects/page.tsx`) |
| `src/apps/teamflow-web/app/org/[slug]/projects/[projectId]/...` | CREATE | Move all project sub-routes |
| `src/apps/teamflow-web/app/org/[slug]/teams/...` | CREATE | Move team routes |
| `src/apps/teamflow-web/app/org/[slug]/settings/page.tsx` | CREATE | Org settings page (name, slug edit) |
| Old route files under `app/projects/`, `app/teams/` | MODIFY | Replace with redirect to `/org/{slug}/...` |

### 4.4 Navigation Components [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/components/layout/org-switcher.tsx` | CREATE | Dropdown showing user's orgs, links to `/org/{slug}/projects` |
| `src/apps/teamflow-web/components/layout/top-bar.tsx` | MODIFY | Add org switcher next to logo |
| `src/apps/teamflow-web/components/layout/breadcrumb.tsx` | MODIFY | Include org name in breadcrumb |

### 4.5 Middleware & Guards [S]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/middleware.ts` | MODIFY | Add `/org/` path handling; Add redirect for old `/projects/` URLs |
| `src/apps/teamflow-web/components/auth/auth-guard.tsx` | MODIFY | Handle org-scoped routes; Redirect authenticated users to org picker if needed |

### 4.6 Admin Dashboard (Frontend) [M] — **completed**

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/admin/layout.tsx` | CREATE | Admin layout with SystemAdmin guard |
| `src/apps/teamflow-web/app/admin/page.tsx` | CREATE | Admin dashboard: org list, user list |
| `src/apps/teamflow-web/app/admin/organizations/page.tsx` | CREATE | Create/list orgs |
| `src/apps/teamflow-web/app/admin/users/page.tsx` | CREATE | User list with system roles |
| `src/apps/teamflow-web/components/admin/admin-guard.tsx` | CREATE | Guard checking `systemRole === 'SystemAdmin'` |

**Phase 4 exit criteria:** All routes under `/org/{slug}/`. Org switcher works. Old URLs redirect. Admin dashboard accessible to SystemAdmin only.

- [x] `OrganizationDto`, `OrgRole`, `InvitationDto`, `OrganizationMemberDto`, `MyOrganizationDto` types added to `types.ts`
- [x] `systemRole` was already in `AuthUser` from Phase 1
- [x] `lib/api/organizations.ts` created: `listMyOrgs()`, `getOrgBySlug()`, `createOrg()`, `updateOrg()`
- [x] `lib/api/invitations.ts` created: full invitation API including `listPendingInvitations()`
- [x] `lib/api/members.ts` created (Phase 6 stub)
- [x] `lib/hooks/use-organizations.ts` created: `useMyOrganizations()`, `useOrganizationBySlug()`
- [x] `lib/hooks/use-invitations.ts` created: `useInvitations()`, `usePendingInvitations()`, `useAcceptInvitation()`, etc.
- [x] `lib/stores/org-store.ts` created: Zustand store for current org context
- [x] `lib/contexts/org-context.tsx` created: React context providing current org
- [x] `app/org/[slug]/layout.tsx` created: async server layout
- [x] `app/org/[slug]/org-layout-client.tsx` created: fetches org by slug, provides OrgContext
- [x] `app/org/[slug]/projects/page.tsx` created: full projects list with org context
- [x] `app/org/[slug]/projects/[projectId]/layout.tsx` + `org-project-layout-client.tsx` created
- [x] All project sub-routes created under `/org/[slug]/projects/[projectId]/` (re-export pattern)
- [x] `app/org/[slug]/teams/page.tsx` created
- [x] `app/org/[slug]/teams/[teamId]/page.tsx` created
- [x] `app/org/[slug]/settings/page.tsx` created: org name/slug edit form
- [x] `components/layout/org-switcher.tsx` created: dropdown with org list
- [x] `components/projects/project-nav.tsx` updated: accepts optional `orgSlug` for org-scoped links
- [x] `components/teams/team-card.tsx` updated: accepts optional `orgSlug`
- [x] `components/projects/create-project-dialog.tsx` updated: accepts `defaultOrgId`, removes hardcoded DEFAULT_ORG_ID
- [x] `app/projects/page.tsx` replaced with org-redirect logic
- [x] `app/teams/page.tsx` replaced with org-redirect logic
- [x] `app/page.tsx` redirects to `/onboarding`
- [x] `middleware.ts` updated: `/invite/` added to public paths
- [x] `components/auth/auth-guard.tsx` updated: authenticated public redirect goes to `/onboarding`
- [x] `app/login/page.tsx` and `app/register/page.tsx` redirect to `/onboarding` after auth
- [x] Admin layout "Back to App" updated to `/onboarding`
- [x] `npm run build` passes with 0 errors

---

## Phase 5: Onboarding Flow — **completed**

**Goal:** Post-login routing based on org membership count. Pending invitations page.
**Dependencies:** Phase 3 complete
**PARALLEL: yes (with Phase 4)**
**Estimated effort:** M

### FILE OWNERSHIP
This phase creates NEW files only. Does not modify any file owned by Phase 4. Coordinates on shared types via Phase 4's `types.ts` additions.

### 5.1 Backend: User Org Count Endpoint [S]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Users/GetCurrentUserTests.cs` | MODIFY: Add org count and pending invitation count to response |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Users/GetCurrentUser/GetCurrentUserHandler.cs` | MODIFY | Add `orgCount` and `pendingInvitationCount` to response |
| `src/core/TeamFlow.Application/Features/Users/UserDto.cs` | MODIFY | Add `OrgCount`, `PendingInvitationCount` fields |

### 5.2 Backend: List Pending Invitations for User [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/Invitations/ListPendingForUserTests.cs` | CREATE: Returns pending invitations matching user email; Excludes expired/revoked/accepted |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/Invitations/ListPendingForUser/ListPendingForUserQuery.cs` | CREATE | Query |
| `src/core/TeamFlow.Application/Features/Invitations/ListPendingForUser/ListPendingForUserHandler.cs` | CREATE | Match by current user email, filter pending + not expired |

### 5.3 Frontend: Onboarding Pages [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/onboarding/page.tsx` | CREATE | Router: 0 orgs -> no-orgs view, 1 org -> redirect to /org/{slug}, N orgs -> org picker |
| `src/apps/teamflow-web/app/onboarding/no-orgs/page.tsx` | CREATE | "No organizations yet" page with pending invitations list |
| `src/apps/teamflow-web/app/onboarding/pick-org/page.tsx` | CREATE | Org picker grid |
| `src/apps/teamflow-web/components/onboarding/pending-invitations.tsx` | CREATE | List pending invitations with accept buttons |
| `src/apps/teamflow-web/components/onboarding/org-picker-card.tsx` | CREATE | Card for org selection |
| `src/apps/teamflow-web/app/invite/[token]/page.tsx` | CREATE | Invitation acceptance page (deep link from shared URL) |

**Phase 5 exit criteria:** Post-login routes to correct page based on org count. Pending invitations visible. Invite deep links work.

- [x] `ListPendingForUserQuery` and `ListPendingForUserHandler` created (TFD: 6 tests pass)
- [x] `IInvitationRepository.ListPendingByEmailAsync()` interface method added
- [x] `InvitationRepository.ListPendingByEmailAsync()` implemented
- [x] `GET /api/v1/invitations/pending` endpoint added to `InvitationsController`
- [x] `lib/api/invitations.ts` includes `listPendingInvitations()` function
- [x] `lib/hooks/use-invitations.ts` includes `usePendingInvitations()` hook
- [x] `app/onboarding/page.tsx` created: router logic (0 orgs → no-orgs, 1 org → direct, N orgs → picker)
- [x] `app/onboarding/no-orgs/page.tsx` created: welcome page with pending invitations
- [x] `app/onboarding/pick-org/page.tsx` created: org picker grid
- [x] `components/onboarding/pending-invitations.tsx` created: renders pending invitations list
- [x] `components/onboarding/org-picker-card.tsx` created: card component for org selection
- [x] `app/invite/[token]/page.tsx` created: deep link invitation acceptance page
- [x] Note: Phase 5.1 (GetCurrentUser orgCount/pendingInvitationCount) deferred — the onboarding router uses `useMyOrganizations()` directly which is equivalent and avoids backend change
- [x] `npm run build` passes with 0 errors

---

## Phase 6: Member Management

**Goal:** Members list, role changes, member removal.
**Dependencies:** Phases 4 and 5 complete
**Estimated effort:** M

### 6.1 Application Layer [M]

**TFD: Write tests first, then implement.**

#### Tests (write first)
| File | What to test |
|------|-------------|
| `tests/TeamFlow.Application.Tests/Features/OrgMembers/ListMembersTests.cs` | CREATE: Any member can list; Non-member gets forbidden |
| `tests/TeamFlow.Application.Tests/Features/OrgMembers/ChangeMemberRoleTests.cs` | CREATE: Owner can change roles; Admin can change Member role; Cannot demote last Owner; Cannot change own role |
| `tests/TeamFlow.Application.Tests/Features/OrgMembers/RemoveMemberTests.cs` | CREATE: Owner/Admin can remove; Cannot remove last Owner; Cannot remove self |

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/core/TeamFlow.Application/Features/OrgMembers/OrgMemberDto.cs` | CREATE | DTO |
| `src/core/TeamFlow.Application/Features/OrgMembers/List/ListOrgMembersQuery.cs` | CREATE | OrgId |
| `src/core/TeamFlow.Application/Features/OrgMembers/List/ListOrgMembersHandler.cs` | CREATE | Check membership, return list |
| `src/core/TeamFlow.Application/Features/OrgMembers/ChangeRole/ChangeOrgMemberRoleCommand.cs` | CREATE | OrgId, UserId, NewRole |
| `src/core/TeamFlow.Application/Features/OrgMembers/ChangeRole/ChangeOrgMemberRoleHandler.cs` | CREATE | Permission check, prevent last-owner demotion |
| `src/core/TeamFlow.Application/Features/OrgMembers/ChangeRole/ChangeOrgMemberRoleValidator.cs` | CREATE | Validate role enum |
| `src/core/TeamFlow.Application/Features/OrgMembers/Remove/RemoveOrgMemberCommand.cs` | CREATE | OrgId, UserId |
| `src/core/TeamFlow.Application/Features/OrgMembers/Remove/RemoveOrgMemberHandler.cs` | CREATE | Permission check, prevent last-owner removal, prevent self-removal |

### 6.2 API Layer [S]

#### Implementation
| File | Action | Description |
|------|--------|-------------|
| `src/apps/TeamFlow.Api/Controllers/OrgMembersController.cs` | CREATE | GET list, PUT change-role, DELETE remove |

### 6.3 Frontend: Members Page [M]

| File | Action | Description |
|------|--------|-------------|
| `src/apps/teamflow-web/app/org/[slug]/members/page.tsx` | CREATE | Members list with role badges |
| `src/apps/teamflow-web/components/org-members/member-list.tsx` | CREATE | Table with role dropdown and remove button |
| `src/apps/teamflow-web/components/org-members/change-role-dialog.tsx` | CREATE | Confirmation dialog for role change |
| `src/apps/teamflow-web/components/org-members/remove-member-dialog.tsx` | CREATE | Confirmation dialog for removal |
| `src/apps/teamflow-web/lib/api/members.ts` | MODIFY | Implement API functions (was stub) |
| `src/apps/teamflow-web/lib/hooks/use-org-members.ts` | CREATE | TanStack Query hooks |

**Phase 6 exit criteria:** Members page shows all org members. Role changes work with guardrails. Member removal works. Cannot remove/demote last Owner.

- [x] `dotnet test` passes (944 tests, 0 failures)
- [x] `IOrganizationMemberRepository` extended with `ListByOrgWithUsersAsync`, `GetByOrgAndUserAsync`, `CountByRoleAsync`, `UpdateAsync`, `DeleteAsync`
- [x] `OrganizationMemberRepository` implements all new methods
- [x] `OrgMemberDto` created: UserId, UserName, UserEmail, Role, JoinedAt
- [x] `ListOrgMembersQuery/Handler` created — any org member can list
- [x] `ChangeOrgMemberRoleCommand/Handler/Validator` created — all guardrails enforced
- [x] `RemoveOrgMemberCommand/Handler` created — all guardrails enforced
- [x] `OrgMembersController` created: GET list, PUT change-role, DELETE remove
- [x] `lib/hooks/use-org-members.ts` created: `useOrgMembers`, `useChangeOrgMemberRole`, `useRemoveOrgMember`
- [x] `components/org-members/member-list.tsx` created: table with role badges and action buttons
- [x] `components/org-members/change-role-dialog.tsx` created: role selection dialog
- [x] `components/org-members/remove-member-dialog.tsx` created: confirmation dialog
- [x] `app/org/[slug]/members/page.tsx` created: full members page
- [x] `npx tsc --noEmit` passes with 0 errors

---

## Integration Phase (Post Phase 6)

### Verification Checklist
- [ ] System admin seeded from config on first startup (idempotent)
- [ ] Only SystemAdmin can create organizations
- [ ] Users belong to multiple orgs via OrganizationMember
- [ ] Invite-based org onboarding with opaque tokens
- [ ] Frontend uses URL-based org routing `/org/{slug}/...`
- [ ] Org switcher in navigation
- [ ] Post-login routing based on org membership count
- [ ] Member management with role assignment
- [ ] Old `/projects/` URLs redirect to `/org/{slug}/projects/`
- [ ] PermissionChecker bootstrap hack removed
- [ ] Last SystemAdmin cannot be deleted
- [ ] Last Org Owner cannot be demoted/removed
- [ ] All existing tests still pass
- [ ] E2E tests cover happy paths for each phase

---

## Planning Notes

### Codebase Observations
1. **Organization does not extend BaseEntity** -- needs to be changed in Phase 2 migration (adds `updated_at`)
2. **PermissionChecker bootstrap hack** (lines 121-128) explicitly documented as security risk -- Phase 2 removes it
3. **OrganizationConfiguration** is missing `created_by_user_id` mapping -- Phase 2 adds it
4. **CreateOrganizationHandler** has no permission check -- Phase 2 adds SystemAdmin requirement
5. **OrganizationsController.GetById** calls repo directly -- violates controller pattern -- Phase 2 fixes it
6. **Frontend has no concept of org context** -- Phase 4 is the most disruptive change

### Migration Safety
The Phase 2 data migration backfills `OrganizationMember(Owner)` for every user who has `ProjectRole.OrgAdmin` on any project in each org. This ensures no access loss. The bootstrap hack removal is safe only after this backfill runs.

### Test Strategy
- All backend phases use TFD: write failing tests first
- Application tests use NSubstitute mocks for repositories
- Integration tests (Infrastructure.Tests) use Testcontainers with real PostgreSQL
- Frontend E2E tests (Playwright) cover route changes and org switching
- Theory patterns for enum validation (SystemRole, OrgRole, InviteStatus)
