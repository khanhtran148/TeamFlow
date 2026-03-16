# Phase 4: Integration

**Depends on:** Phases 1, 2, 3 all complete

---

## Summary

Wire frontend to real backend. Verify all tabs work end-to-end. Fix any mismatches between contract and implementation.

---

## Tasks

### 4.1 Remove any mock data from frontend

If Phase 3 used mocked responses during parallel development, replace with real API calls.

### 4.2 End-to-end verification

Manual verification checklist:
- [ ] Load `/profile` -- Details tab shows real user data from `GET /me/profile`
- [ ] Edit name, save -- `PUT /me/profile` succeeds, UI updates
- [ ] Edit avatarUrl, save -- avatar displays or clears
- [ ] Security tab: change password -- `POST /auth/change-password` succeeds
- [ ] Notifications tab: toggle a preference, save -- `PUT /notifications/preferences` succeeds
- [ ] Activity tab: shows real activity from `GET /me/activity`, pagination works
- [ ] UserMenu shows "Profile" link, navigates correctly

### 4.3 Auth store sync

After profile update, ensure the auth store's `user.name` reflects the change. Options:
- Refetch current user after profile update mutation succeeds
- Or update auth store directly from mutation's response data

### 4.4 Edge cases

- [ ] New user with no org memberships or team memberships -- empty state renders
- [ ] User with no activity history -- empty state on activity tab
- [ ] AvatarUrl set to null -- falls back to initials avatar

---

## Acceptance Criteria

- [ ] All four profile tabs work against real API
- [ ] No mock data remains in production code
- [ ] Auth store stays in sync after profile edits
- [ ] Edge cases handled gracefully
