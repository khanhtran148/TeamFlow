# Implement State: Phase 6 — Member Management

## Topic
Implement Phase 6 (Member Management) of the org management feature — the final phase.

## Discovery Context

- **Branch:** `feat/org-management-admin-bootstrap` (continuing)
- **Feature Scope:** Fullstack (backend handlers + API + frontend pages)
- **Task Type:** feature
- **Test DB Strategy:** Docker containers (Testcontainers with real PostgreSQL)

## Requirements

### Application Layer (TFD — tests first)

#### Tests to Create
- `tests/TeamFlow.Application.Tests/Features/OrgMembers/ListMembersTests.cs`:
  - Any org member can list members
  - Non-member gets forbidden
- `tests/TeamFlow.Application.Tests/Features/OrgMembers/ChangeMemberRoleTests.cs`:
  - Owner can change any member's role
  - Admin can change Member's role to Admin
  - Cannot demote last Owner
  - Cannot change own role
  - Member cannot change roles
- `tests/TeamFlow.Application.Tests/Features/OrgMembers/RemoveMemberTests.cs`:
  - Owner/Admin can remove members
  - Cannot remove last Owner
  - Cannot remove self
  - Member cannot remove others

#### Handlers to Implement
- `src/core/TeamFlow.Application/Features/OrgMembers/OrgMemberDto.cs` — DTO: Id, UserId, UserName, UserEmail, Role, JoinedAt
- `src/core/TeamFlow.Application/Features/OrgMembers/List/ListOrgMembersQuery.cs` — OrgId
- `src/core/TeamFlow.Application/Features/OrgMembers/List/ListOrgMembersHandler.cs` — Check user is org member, return list
- `src/core/TeamFlow.Application/Features/OrgMembers/ChangeRole/ChangeOrgMemberRoleCommand.cs` — OrgId, UserId, NewRole (OrgRole)
- `src/core/TeamFlow.Application/Features/OrgMembers/ChangeRole/ChangeOrgMemberRoleHandler.cs`:
  - Check current user is Owner or Admin
  - Admin cannot promote to Owner
  - Cannot demote last Owner (count Owners in org, if target is Owner and count == 1, reject)
  - Cannot change own role
  - Update role
- `src/core/TeamFlow.Application/Features/OrgMembers/ChangeRole/ChangeOrgMemberRoleValidator.cs` — Validate role enum
- `src/core/TeamFlow.Application/Features/OrgMembers/Remove/RemoveOrgMemberCommand.cs` — OrgId, UserId
- `src/core/TeamFlow.Application/Features/OrgMembers/Remove/RemoveOrgMemberHandler.cs`:
  - Check current user is Owner or Admin
  - Cannot remove last Owner
  - Cannot remove self
  - Delete org membership

### Infrastructure Changes
- `IOrganizationMemberRepository` — May need additional methods:
  - `ListByOrgAsync(orgId)` — return all members with User navigation loaded
  - `CountByRoleAsync(orgId, role)` — count members with a specific role
  - `DeleteAsync(member)` — remove membership
- `OrganizationMemberRepository` — Implement new methods

### API Layer
- `src/apps/TeamFlow.Api/Controllers/OrgMembersController.cs` — CREATE:
  - `GET /api/v1/organizations/{orgId}/members` — list members
  - `PUT /api/v1/organizations/{orgId}/members/{userId}/role` — change role (body: { role: "Admin" })
  - `DELETE /api/v1/organizations/{orgId}/members/{userId}` — remove member

### Frontend
- `src/apps/teamflow-web/app/org/[slug]/members/page.tsx` — Members list with role badges
- `src/apps/teamflow-web/components/org-members/member-list.tsx` — Table with role dropdown and remove button
- `src/apps/teamflow-web/components/org-members/change-role-dialog.tsx` — Confirmation dialog
- `src/apps/teamflow-web/components/org-members/remove-member-dialog.tsx` — Confirmation dialog
- `src/apps/teamflow-web/lib/api/members.ts` — MODIFY: implement actual API calls (was stub)
- `src/apps/teamflow-web/lib/hooks/use-org-members.ts` — CREATE: TanStack Query hooks

### Business Rules (Guardrails)
1. Only Owner and Admin can manage members
2. Admin CANNOT promote anyone to Owner (only Owner can)
3. Cannot demote the LAST Owner (org must always have at least one Owner)
4. Cannot remove the LAST Owner
5. Cannot change your own role
6. Cannot remove yourself
7. Any org member can view the members list

## Phase-Specific Context
- **Plan directory:** docs/plans/org-management-admin-bootstrap
- **Plan source:** docs/plans/org-management-admin-bootstrap/plan.md (Phase 6)
- **ADR:** docs/adrs/260316-org-management-admin-bootstrap.md
