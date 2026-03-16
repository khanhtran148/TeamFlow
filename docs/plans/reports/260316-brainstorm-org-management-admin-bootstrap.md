# Brainstorm Report: Org Management & Admin Bootstrap

**Date:** 2026-03-16
**ADR:** `docs/adrs/260316-org-management-admin-bootstrap.md`
**Discovery Context:** `docs/brainstorms/org-management-and-admin-bootstrap-20260316/discovery-context.md`

## Problem Statement

TeamFlow has no organization management or system admin capabilities. Users register but cannot create or join orgs. No admin account exists on first launch. The frontend uses a hardcoded org ID. The permission system has a bootstrap hack that grants implicit admin access.

## Scope

Full org lifecycle: admin bootstrap, org CRUD, membership model, invitation system, multi-org frontend UX, onboarding flow, and member management.

## Key Decisions

| Decision | Choice | Score |
|---|---|---|
| SystemRole storage | Enum on User entity | 4.25/5 |
| Org membership | Dedicated OrganizationMember table | 4.50/5 |
| Invitation tokens | Database-backed opaque tokens | 4.75/5 |
| Frontend org context | URL-based `/org/{slug}/...` routing | 4.25/5 |
| Admin dashboard | Separate `/admin` route | 4.50/5 |

## Implementation Phases

### Phase 1: System Admin Bootstrap
- Add `SystemRole` enum to User entity
- `AdminSeedService : IHostedService` reads config, seeds admin on startup
- System admin API endpoints (`/api/v1/admin/organizations`, `/api/v1/admin/users`)
- `[SystemAdminOnly]` authorization attribute

### Phase 2: Org Membership Model
- `OrganizationMember` entity with `OrgRole` enum (Owner/Admin/Member)
- `Slug` property on Organization (unique, URL-safe)
- Org CRUD endpoints (admin-only create, owner/admin update/delete)
- Refactor `PermissionChecker` to check org membership
- Data migration: backfill from existing ProjectMembership OrgAdmin records

### Phase 3: Invitation System
- `Invitation` entity with opaque hashed token, 7-day expiry
- Create/accept/revoke/list endpoints
- Status tracking: Pending → Accepted/Expired/Revoked
- Email-less for MVP — share invite links manually

### Phase 4: Frontend Multi-Org UX
- Route restructure: `/org/{slug}/projects/...`
- Org switcher component in top navigation
- Remove all `DEFAULT_ORG_ID` references
- Redirect middleware for old URLs
- `GET /api/v1/me/organizations` for current user's memberships

### Phase 5: Onboarding Flow
- Post-login redirect: 0 orgs → "no orgs" page, 1 org → projects, multiple → org picker
- Pending invitations page after registration
- Empty state UX with guidance

### Phase 6: Member Management
- Members list page at `/org/{slug}/members`
- Role assignment (OrgAdmin/Owner can change roles)
- Remove member endpoint (soft removal)

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Users lose access during migration | High | High | Backfill OrganizationMember from ProjectMembership |
| Seeded admin deleted | Low | Critical | Prevent last SystemAdmin deletion + CLI escape hatch |
| Frontend route regressions | Medium | Medium | Redirect middleware + E2E tests |
| Invitation token leaked | Medium | Medium | Opaque tokens, short-lived, one-time-use |

## Accepted Trade-offs

1. Every frontend route changes — accepted for multi-tab and deep-link correctness
2. Two membership tables coexist — accepted because they serve different scopes
3. No email delivery for MVP invites — accepted to avoid SMTP dependency
4. Slug uniqueness is first-come-first-served — accepted with reserved list and auto-suggestions
