---
topic: User Profile Management
status: complete
created: 2026-03-16
---

# Implement State: User Profile Management

## Discovery Context
- **Branch:** feat/user-profile (created from main)
- **Requirements:** Full profile management — view, edit, change password, notification prefs, activity log
- **Test DB Strategy:** In-memory/mocks
- **Feature Scope:** Fullstack
- **Task Type:** feature

## Phase-Specific Context
- **Plan dir:** docs/plans/user-profile
- **Plan source:** docs/plans/user-profile/plan.md
- **API contract:** docs/plans/user-profile/api-contract-260316-1600.md

### Plan Summary
4 phases:
1. **Phase 1 (Backend):** Add AvatarUrl to User + migration, GetProfile query, UpdateProfile command — TFD
2. **Phase 2 (Backend):** GetActivityLog paginated query from work_item_histories — TFD
3. **Phase 3 (Frontend):** /profile page with 4 tabs (Details, Security, Notifications, Activity), API client, hooks, UserMenu link
4. **Phase 4 (Integration):** Wire FE to real BE, verify end-to-end

### Key Decisions
- Avatar is URL string, no file upload
- Activity from existing work_item_histories table
- Reuse existing change-password and notification-preferences endpoints
- No permission check — users access only their own profile
- PageSize capped at 50 for activity log
