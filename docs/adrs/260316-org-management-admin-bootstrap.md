# ADR: Organization Management & Admin Bootstrap

**Date:** 2026-03-16
**Status:** Accepted
**Deciders:** Development team
**Context:** TeamFlow brainstorm session

## Context

TeamFlow lacks organization management and system admin capabilities. After registration, users cannot create or join organizations. No system-wide admin exists. The frontend uses a hardcoded `DEFAULT_ORG_ID`. The `PermissionChecker` has a bootstrap hack that implicitly grants admin access when no OrgAdmin exists — a security anti-pattern.

## Decision

Implement a 6-cluster phased approach:

### D1. SystemRole enum on User entity
Add `SystemRole` (User | SystemAdmin) to the User entity. Seed the first SystemAdmin from `appsettings.json` via an `IHostedService` on startup. Idempotent — skips if the admin email already exists.

**Rejected:** Separate SystemAdmin table (adds unnecessary joins, breaks convention that identity lives on User).

### D2. Dedicated OrganizationMember table
Create `OrganizationMember(UserId, OrgId, OrgRole)` with `OrgRole` enum (Owner | Admin | Member). Replace the `ProjectRole.OrgAdmin` conflation with proper org-level membership.

**Rejected:** Extending ProjectMembership (fails when org has zero projects, overloads an already complex table).

### D3. Database-backed invite tokens
Create `Invitation` entity with opaque hashed tokens, 7-day expiry, status tracking (Pending/Accepted/Expired/Revoked). Email-less for MVP — share links manually.

**Rejected:** JWT invite tokens (payload visible in URL, revocation requires blocklist, no benefit at this scale).

### D4. URL-based org routing
Frontend routes become `/org/{slug}/projects/...`. Org context lives in the URL, not in Zustand state.

**Rejected:** Zustand + cookie (breaks bookmarks, back-button, multi-tab scenarios).

### D5. Separate /admin route
System admin dashboard at `/admin` for platform-wide operations, separate from org settings.

**Rejected:** Embedded in org settings (mixes platform and org concerns, creates permission complexity).

## Implementation Order

1. System Admin Bootstrap (SystemRole + seed + admin endpoints)
2. Org Membership Model (OrganizationMember + OrgRole + PermissionChecker refactor)
3. Invitation System (Invitation entity + create/accept/revoke endpoints)
4. Frontend Multi-Org UX (URL routing + org switcher + remove DEFAULT_ORG_ID)
5. Onboarding Flow (post-login redirect + pending invitations page)
6. Member Management (members list + role change + remove member)

## Consequences

### Positive
- Explicit admin bootstrap replaces implicit permission hack
- Org-level membership enables multi-org support from day one
- URL-based routing prevents multi-tab and deep-linking bugs
- Invitation system provides controlled org onboarding

### Negative
- Every existing frontend route changes (migration cost)
- Two membership tables coexist (OrganizationMember + ProjectMembership)
- Data migration needed to backfill OrganizationMember from existing ProjectMembership OrgAdmin records

### Risks
- Existing users may lose access during migration → mitigated by data backfill migration
- Seeded admin deletion locks the system → mitigated by preventing last SystemAdmin deletion + CLI escape hatch
- Frontend route migration may introduce regressions → mitigated by redirect middleware + E2E tests

## New Domain Types

```
enum SystemRole { User, SystemAdmin }
enum OrgRole { Owner, Admin, Member }
enum InviteStatus { Pending, Accepted, Expired, Revoked }

sealed class OrganizationMember : BaseEntity
sealed class Invitation : BaseEntity

Modified: User (add SystemRole), Organization (add Slug)
```
