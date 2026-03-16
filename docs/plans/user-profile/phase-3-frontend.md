# Phase 3: Frontend -- Profile Page

**PARALLEL:** yes (with Phases 1 & 2)
**Depends on:** API Contract (api-contract-260316-1600.md)
**Note:** Can start with mocked API responses, wire to real backend once Phases 1-2 merge.

---

## Summary

Build the `/profile` page with four tabs: Details, Security, Notifications, Activity. Add profile link to UserMenu. Create API client functions and TanStack Query hooks.

---

## FILE OWNERSHIP

This phase owns all files under:
- `src/apps/teamflow-web/app/profile/` (new)
- `src/apps/teamflow-web/components/profile/` (new)
- `src/apps/teamflow-web/lib/api/users.ts` (modify -- add profile functions)
- `src/apps/teamflow-web/lib/api/types.ts` (modify -- add profile types)
- `src/apps/teamflow-web/lib/hooks/use-profile.ts` (new)
- `src/apps/teamflow-web/components/layout/user-menu.tsx` (modify -- add profile link)

---

## Tasks

### 3.1 TypeScript Types

Add to `src/apps/teamflow-web/lib/api/types.ts`:
- `UserProfileDto`
- `ProfileOrganizationDto`
- `ProfileTeamDto`
- `UpdateProfileBody`
- `ActivityLogItemDto`

(Exact shapes in api-contract-260316-1600.md)

### 3.2 API Client Functions

Add to `src/apps/teamflow-web/lib/api/users.ts`:

```typescript
export async function getProfile(): Promise<UserProfileDto>
export async function updateProfile(body: UpdateProfileBody): Promise<UserProfileDto>
export async function getActivityLog(params: { page?: number; pageSize?: number }): Promise<PagedResult<ActivityLogItemDto>>
```

### 3.3 TanStack Query Hooks

**File:** `src/apps/teamflow-web/lib/hooks/use-profile.ts`

```typescript
export function useProfile()              // useQuery wrapping getProfile
export function useUpdateProfile()        // useMutation wrapping updateProfile, invalidates profile query
export function useActivityLog(page, pageSize) // useQuery wrapping getActivityLog
```

### 3.4 Profile Page Layout

**File:** `src/apps/teamflow-web/app/profile/page.tsx`

Tabbed layout with four tabs:
- Details (default)
- Security
- Notifications
- Activity

Use URL search params or local state for tab selection. Wrap in auth guard.

### 3.5 Details Tab Component

**File:** `src/apps/teamflow-web/components/profile/profile-details.tsx`

Shows:
- Avatar (initials fallback when no avatarUrl)
- Name (editable inline or via edit mode)
- Email (read-only, shown but not editable)
- System role badge (read-only)
- Member since date
- Organization memberships list (org name, role, joined date)
- Team memberships list (team name, project name, role)

Edit mode:
- Name input field
- Avatar URL input field
- Save / Cancel buttons
- Calls `useUpdateProfile` mutation on save

### 3.6 Security Tab Component

**File:** `src/apps/teamflow-web/components/profile/profile-security.tsx`

Change password form with:
- Current password field
- New password field
- Confirm new password field
- Submit button

Calls existing `changePassword` from `lib/api/auth.ts`. Show success/error toast.

### 3.7 Notifications Tab

**File:** `src/apps/teamflow-web/components/profile/profile-notifications.tsx`

Thin wrapper that renders the existing `NotificationPreferences` component from `components/notifications/notification-preferences.tsx`.

### 3.8 Activity Tab Component

**File:** `src/apps/teamflow-web/components/profile/profile-activity.tsx`

Shows:
- Paginated list of recent actions
- Each row: action type icon/badge, work item title (link to work item), field changed, old/new values, timestamp
- Pagination controls (previous/next)
- Empty state when no activity

### 3.9 UserMenu Profile Link

Modify `src/apps/teamflow-web/components/layout/user-menu.tsx`:
- Add "Profile" menu item between the user info header and "Sign out" button
- Uses `User` icon from lucide-react (already imported)
- Links to `/profile`

---

## Acceptance Criteria

- [ ] `/profile` page loads with four tabs
- [ ] Details tab shows user info, orgs, teams; allows editing name and avatarUrl
- [ ] Security tab allows password change using existing endpoint
- [ ] Notifications tab renders existing preferences component
- [ ] Activity tab shows paginated activity feed
- [ ] UserMenu dropdown has "Profile" link
- [ ] Profile page protected by auth guard
- [ ] After profile update, the auth store's user.name updates (optimistic or refetch)
