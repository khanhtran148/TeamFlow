# Discovery Context: Org Management & Admin Bootstrap

**Date:** 2026-03-16
**Topic:** Organization management, admin bootstrap, and multi-org support for TeamFlow

## Scope
Full org lifecycle + admin: Org CRUD, member invite/management, role assignment, admin dashboard, system admin bootstrap, onboarding wizard.

## Admin Bootstrap Strategy
Seed via config — Admin email/password defined in appsettings.json or environment variables, auto-created on first startup. Deterministic and DevOps-friendly.

## Org Creation Policy
Admin-only — Only system admins can create organizations. Fits internal team tool (9–15 people).

## Multi-Org Strategy
Multi-org from day one — Users can belong to multiple orgs. Org switcher in UI navigation. All features scoped to selected org context.

## Current Gaps (from Scout)
1. No system-wide admin role — only per-project OrgAdmin
2. Implicit admin bootstrap via PermissionChecker side-effect
3. Hardcoded DEFAULT_ORG_ID in frontend
4. No org management UI or API (invite, roles, settings)
5. No seeding/initialization logic
6. No onboarding flow for first-time users
