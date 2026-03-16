# Implement State: Phase 3 — Invitation System

## Topic
Implement Phase 3 (Invitation System) of the org management feature.

## Discovery Context

- **Branch:** `feat/org-management-admin-bootstrap` (continuing)
- **Feature Scope:** Backend-only
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)

## Requirements

### Domain Changes
1. `InviteStatus` enum: Pending, Accepted, Expired, Revoked
2. `Invitation` sealed entity: Id, OrganizationId, InvitedByUserId, Email (optional), Role (OrgRole), TokenHash, Status (InviteStatus), ExpiresAt, CreatedAt, AcceptedAt (nullable), AcceptedByUserId (nullable)

### Infrastructure Changes
1. `InvitationConfiguration`: table `invitations`, index on `token_hash`
2. `TeamFlowDbContext`: add `DbSet<Invitation>`
3. `InvitationRepository`: Add, GetByTokenHashAsync, ListByOrgAsync, GetByIdAsync, UpdateAsync
4. `DependencyInjection`: register IInvitationRepository
5. EF Migration: create `invitations` table

### Application Layer
1. `IInvitationRepository` interface
2. `InvitationDto`: Id, OrganizationId, OrgName, Email, Role, Status, ExpiresAt, CreatedAt (NO raw token in DTO)
3. `CreateInvitationCommand/Handler/Validator`:
   - Only Org Owner/Admin can create (check via IOrganizationMemberRepository)
   - Generate cryptographically random token (32 bytes → base64url)
   - Hash token with SHA-256 before storing
   - Return the raw token ONLY in the create response (not stored)
   - Set ExpiresAt = now + 7 days
   - Set Status = Pending
4. `AcceptInvitationCommand/Handler`:
   - Input: raw token string
   - Hash the token, look up by TokenHash
   - Check: not expired, status is Pending
   - Check: user not already a member of the org
   - Create OrganizationMember with the invitation's Role
   - Update invitation: Status=Accepted, AcceptedAt=now, AcceptedByUserId=currentUser
   - Return org info for redirect
5. `RevokeInvitationCommand/Handler`:
   - Only Org Owner/Admin can revoke
   - Cannot revoke already-accepted invitations
   - Set Status = Revoked
6. `ListInvitationsQuery/Handler`:
   - Only Org Owner/Admin can list
   - Return all invitations for the org (filter by status optional)

### API Layer
1. `InvitationsController`:
   - `POST /api/v1/organizations/{orgId}/invitations` — create (returns raw token in response)
   - `GET /api/v1/organizations/{orgId}/invitations` — list
   - `POST /api/v1/invitations/{token}/accept` — accept (authenticated)
   - `DELETE /api/v1/invitations/{id}` — revoke
   - `GET /api/v1/invitations/{token}` — get invite details for preview (public, shows org name + role, no sensitive data)

### Test Files
- `tests/TeamFlow.Domain.Tests/EnumTests.cs` — add InviteStatus coverage
- `tests/TeamFlow.Infrastructure.Tests/Repositories/InvitationRepositoryTests.cs` — CRUD, GetByTokenHash
- `tests/TeamFlow.Application.Tests/Features/Invitations/CreateInvitationTests.cs` — permission, token generation, expiry
- `tests/TeamFlow.Application.Tests/Features/Invitations/AcceptInvitationTests.cs` — valid/expired/accepted/revoked/already-member
- `tests/TeamFlow.Application.Tests/Features/Invitations/RevokeInvitationTests.cs` — permission, status checks
- `tests/TeamFlow.Application.Tests/Features/Invitations/ListInvitationsTests.cs` — permission, returns org invites
- `tests/TeamFlow.Tests.Common/Builders/InvitationBuilder.cs` — new builder

### Security Considerations
- Token is 32 bytes of cryptographic randomness (use RandomNumberGenerator)
- Only the SHA-256 hash is stored in DB
- Raw token returned ONLY on creation, never again
- Token in URL is opaque (no payload leakage)
- Expired tokens must be rejected (check ExpiresAt vs UTC now)
- Accepted tokens cannot be reused
- Role in invitation cannot exceed inviter's role (optional guardrail)

## Phase-Specific Context
- **Plan directory:** docs/plans/org-management-admin-bootstrap
- **Plan source:** docs/plans/org-management-admin-bootstrap/plan.md (Phase 3 section)
- **ADR:** docs/adrs/260316-org-management-admin-bootstrap.md
- **API contract:** docs/plans/org-management-admin-bootstrap/api-contract-260316-1500.md
