# Plan: User Profile Management

**Created:** 2026-03-16
**Scope:** Fullstack (Backend + Frontend + API)
**Status:** Awaiting approval

---

## Overview

Add a full user profile management experience to TeamFlow. Users can view and edit their profile, change their password, manage notification preferences, and review their recent activity -- all from a single `/profile` page.

---

## What Exists Today

- **User entity** has id, email, name, systemRole, isActive -- no avatar field
- **GET /api/v1/users/me** returns lightweight DTO (id, email, name, orgs list)
- **POST /api/v1/auth/change-password** fully implemented
- **Notification preferences** (GET + PUT) fully implemented
- **UserMenu component** shows name/email/logout -- no profile link
- **No profile page** exists

## What This Plan Adds

1. `AvatarUrl` property on User entity + migration
2. `GET /me/profile` -- rich profile with orgs (with roles), teams, systemRole, createdAt
3. `PUT /me/profile` -- update name and avatarUrl
4. `GET /me/activity` -- paginated activity log from work_item_histories
5. `/profile` page with tabs: Details, Security, Notifications, Activity
6. "Profile" link in UserMenu dropdown

---

## Phase Structure

```
Phase 0: Contract (api-contract-260316-1600.md) -- DONE
    |
    +-- Phase 1: Backend - GetProfile + UpdateProfile (TFD)  ──┐
    |                                                           ├── PARALLEL
    +-- Phase 2: Backend - GetActivityLog (TFD)                 │
    |                                                           │
    +-- Phase 3: Frontend - Profile page + all tabs  ───────────┘
    |
Phase 4: Integration -- wire FE to real BE, verify end-to-end
```

**Phases 1 and 2** are sequential (Phase 2 modifies UsersController after Phase 1).
**Phase 3** can run in parallel with Phases 1-2 using mocked API responses.
**Phase 4** runs after all others complete.

---

## Phase Files

| Phase | File | Description |
|-------|------|-------------|
| Contract | `api-contract-260316-1600.md` | Endpoint specs, DTOs, TypeScript types |
| 1 | `phase-1-backend-profile.md` | AvatarUrl migration, GetProfile, UpdateProfile |
| 2 | `phase-2-backend-activity.md` | GetActivityLog paginated query |
| 3 | `phase-3-frontend.md` | Profile page, tabs, hooks, UserMenu link |
| 4 | `phase-4-integration.md` | Wire FE to BE, end-to-end verification |

---

## Key Decisions

1. **No file upload for avatar.** AvatarUrl is a plain URL string. Users paste a URL (e.g., Gravatar, GitHub avatar). File upload can be added later if needed.

2. **Activity log sources from work_item_histories only.** This table already tracks who did what to which work item. No need to introduce a separate activity table.

3. **Reuse existing endpoints** for password change and notification preferences. The profile page composes these existing features into a single UI.

4. **No permission check needed** on profile endpoints -- users can only view/edit their own profile via `ICurrentUser.Id`.

5. **PageSize capped at 50** for activity log to prevent large payloads.

---

## New Files Summary

### Backend (13 files)
- `src/core/TeamFlow.Domain/Entities/User.cs` (modify)
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs` (modify)
- `src/core/TeamFlow.Infrastructure/Migrations/*_AddAvatarUrlToUser.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/UserProfileDto.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/GetProfile/GetProfileQuery.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/GetProfile/GetProfileHandler.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/UpdateProfileCommand.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/UpdateProfileValidator.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/UpdateProfileHandler.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/GetActivityLog/GetActivityLogQuery.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/GetActivityLog/GetActivityLogHandler.cs` (new)
- `src/core/TeamFlow.Application/Features/Users/ActivityLogItemDto.cs` (new)
- `src/apps/TeamFlow.Api/Controllers/UsersController.cs` (modify)

### Backend Tests (4 files)
- `tests/TeamFlow.Application.Tests/Features/Users/GetProfile/GetProfileHandlerTests.cs` (new)
- `tests/TeamFlow.Application.Tests/Features/Users/UpdateProfile/UpdateProfileHandlerTests.cs` (new)
- `tests/TeamFlow.Application.Tests/Features/Users/UpdateProfile/UpdateProfileValidatorTests.cs` (new)
- `tests/TeamFlow.Application.Tests/Features/Users/GetActivityLog/GetActivityLogHandlerTests.cs` (new)

### Frontend (9 files)
- `src/apps/teamflow-web/lib/api/types.ts` (modify)
- `src/apps/teamflow-web/lib/api/users.ts` (modify)
- `src/apps/teamflow-web/lib/hooks/use-profile.ts` (new)
- `src/apps/teamflow-web/app/profile/page.tsx` (new)
- `src/apps/teamflow-web/components/profile/profile-details.tsx` (new)
- `src/apps/teamflow-web/components/profile/profile-security.tsx` (new)
- `src/apps/teamflow-web/components/profile/profile-notifications.tsx` (new)
- `src/apps/teamflow-web/components/profile/profile-activity.tsx` (new)
- `src/apps/teamflow-web/components/layout/user-menu.tsx` (modify)

---

## Estimated Effort

- Phase 1: ~2-3 hours (migration + 2 handlers + tests)
- Phase 2: ~1-2 hours (1 handler + tests)
- Phase 3: ~3-4 hours (page + 4 tab components + hooks + API client)
- Phase 4: ~1 hour (integration wiring + verification)
- **Total: ~7-10 hours**
