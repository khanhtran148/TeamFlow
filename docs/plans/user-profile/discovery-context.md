---
type: discovery-context
feature: User Profile Management
created: 2026-03-16
---

# Discovery Context — User Profile Management

## Scope
**Fullstack** — Frontend + Backend + API

## Requirements
Full profile management: view profile details, edit name/avatar, change password, manage notification preferences, and view activity history.

## Success Criteria
- User can view all their details (name, email, role, org memberships, avatar)
- User can update name and avatar with validation
- User can change password (existing ChangePassword endpoint)
- User can manage notification preferences (existing endpoints)
- User can view their recent activity log
- All features have tests (unit + integration)

## Existing State
### Backend
- **User entity** (`Domain/Entities/User.cs`): id, email, passwordHash, name, systemRole, mustChangePassword, isActive
- **GetCurrentUser** (`Features/Users/GetCurrentUser/`): returns id, email, name, orgs — no avatar, no role, no activity
- **ChangePassword** (`Features/Auth/ChangePassword/`): already implemented
- **Notification preferences** (`Features/Notifications/GetPreferences/` + `UpdatePreferences/`): already implemented
- **UsersController** (`Api/Controllers/UsersController.cs`): only has `GET /me`
- **AuthController** (`Api/Controllers/AuthController.cs`): has change-password

### Frontend
- **UserMenu** (`components/layout/user-menu.tsx`): shows name, email, avatar initials, logout — no profile link
- **No profile page** exists (`app/profile/` does not exist)
- **Notification preferences** component exists (`components/notifications/notification-preferences.tsx`)
- **Auth store** provides user object

## What Needs Building
1. **Backend: UpdateProfile command** — update name (avatar URL if we add it)
2. **Backend: GetProfile query** — richer profile data (orgs, teams, role, created_at, activity)
3. **Backend: GetActivityLog query** — user's recent actions from work_item_histories + domain_events
4. **Frontend: Profile page** — `/profile` route with tabs (Details, Security, Notifications, Activity)
5. **Frontend: Edit profile form** — name update with validation
6. **Frontend: Wire existing features** — change password + notification prefs into profile page
