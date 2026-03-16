# Implement State: Phase 2 — Org Membership Model

## Topic
Implement Phase 2 (Org Membership Model) of the org management feature.

## Discovery Context

- **Branch:** `feat/org-management-admin-bootstrap` (already on it, continuing from Phase 1)
- **Feature Scope:** Backend-only (Phase 2 has no frontend tasks)
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)

## Requirements

### Domain Changes
1. `OrgRole` enum: Owner, Admin, Member
2. `OrganizationMember` sealed class: OrganizationId, UserId, Role (OrgRole), JoinedAt
3. `Organization` entity: inherit from BaseEntity, add `Slug` property (unique, URL-safe), add `ICollection<OrganizationMember> Members` navigation

### Infrastructure Changes
1. `OrganizationConfiguration`: add `slug` column (unique index), `created_by_user_id` mapping, `updated_at`
2. `OrganizationMemberConfiguration`: table `organization_members`, composite unique index on `(organization_id, user_id)`
3. `TeamFlowDbContext`: add `DbSet<OrganizationMember>`
4. `OrganizationMemberRepository`: CRUD operations
5. `OrganizationRepository`: add `GetBySlugAsync`, `ListByMembershipAsync`
6. `PermissionChecker`: REPLACE bootstrap hack with OrganizationMember lookup. The bootstrap hack is in IsOrgAdminAsync (lines ~118-128). Replace it with: check if user has OrgRole.Owner or OrgRole.Admin in OrganizationMember table for the given org.
7. `DependencyInjection`: register IOrganizationMemberRepository

### Application Layer
1. `IOrganizationRepository`: add `GetBySlugAsync`, `ExistsBySlugAsync`
2. `IOrganizationMemberRepository`: new interface for org member operations
3. `CreateOrganizationHandler`: add SystemAdmin check, generate slug from name, create OrganizationMember(Owner) for the user specified as creator
4. `CreateOrganizationValidator`: add slug uniqueness validation
5. `UpdateOrganizationCommand/Handler/Validator`: new — Org Owner/Admin can update name/slug
6. `GetOrganizationBySlugQuery/Handler`: new — check org membership before returning
7. `ListMyOrganizationsQuery/Handler`: new — return orgs where current user is a member
8. `OrganizationDto`: add Slug property

### API Layer
1. `OrganizationsController`: add PUT update, GET by-slug, GET me/organizations endpoints. Fix GetById to use a handler instead of calling repository directly.

### Data Migration
1. Add `slug` (varchar, unique) and `updated_at` (timestamptz) columns to `organizations` table
2. Create `organization_members` table with composite unique index
3. Backfill: for each user with `ProjectRole.OrgAdmin` on any project in an org, create `OrganizationMember(Owner)`
4. Generate slug from org name (lowercase, hyphens, strip special chars)

### TFD Workflow (Non-Negotiable)
1. Write failing tests FIRST for each component
2. Implement minimal code to make tests pass
3. Refactor while green

### Test Files to Create/Modify
- `tests/TeamFlow.Domain.Tests/EnumTests.cs` — add OrgRole coverage
- `tests/TeamFlow.Domain.Tests/Entities/OrganizationTests.cs` — slug generation
- `tests/TeamFlow.Infrastructure.Tests/Repositories/OrganizationMemberRepositoryTests.cs` — CRUD
- `tests/TeamFlow.Infrastructure.Tests/Services/PermissionCheckerTests.cs` — remove bootstrap hack tests, add org membership tests
- `tests/TeamFlow.Application.Tests/Features/Organizations/CreateOrganizationTests.cs` — require SystemAdmin, auto-create Owner
- `tests/TeamFlow.Application.Tests/Features/Organizations/UpdateOrganizationTests.cs` — permission checks
- `tests/TeamFlow.Application.Tests/Features/Organizations/GetOrganizationBySlugTests.cs` — membership check
- `tests/TeamFlow.Application.Tests/Features/Organizations/ListMyOrganizationsTests.cs` — returns member orgs

### Test Infrastructure
- `tests/TeamFlow.Tests.Common/Builders/OrganizationBuilder.cs` — add WithSlug()
- `tests/TeamFlow.Tests.Common/Builders/OrganizationMemberBuilder.cs` — new builder
- `tests/TeamFlow.Tests.Common/TestDataSeeder.cs` — seed OrganizationMember for test user
- `tests/TeamFlow.Tests.Common/IntegrationTestBase.cs` — register IOrganizationMemberRepository

## Phase-Specific Context

- **Plan directory:** docs/plans/org-management-admin-bootstrap
- **Plan source:** docs/plans/org-management-admin-bootstrap/plan.md (Phase 2 section)
- **ADR:** docs/adrs/260316-org-management-admin-bootstrap.md
- **API contract:** docs/plans/org-management-admin-bootstrap/api-contract-260316-1500.md
