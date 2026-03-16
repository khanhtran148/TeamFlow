# Plan: Admin Action Confirmation Dialogs

**Scope:** Frontend-only
**Goal:** Every mutating action on `/admin` pages requires explicit user confirmation before executing.

---

## Current State

| Action | Page | Has Confirmation? |
|---|---|---|
| Edit Organization | `/admin/organizations` | Yes (form dialog) |
| Transfer Ownership | `/admin/organizations` | Yes (form dialog) |
| Reset Password | `/admin/users` | Yes (form dialog) |
| Toggle Org Status | `/admin/organizations` | **No** |
| Toggle User Status | `/admin/users` | **No** |

The three existing dialogs are **form dialogs** (they collect input). The two toggle actions fire immediately on click with no confirmation.

---

## Tasks

### 1. Create reusable `ConfirmDialog` component

**File:** `components/admin/confirm-dialog.tsx`

Props:
- `open: boolean`
- `title: string`
- `message: string`
- `confirmLabel?: string` (default: "Confirm")
- `confirmVariant?: "danger" | "default"` (controls button color)
- `onConfirm: () => void`
- `onCancel: () => void`
- `loading?: boolean`

Style: Match existing dialog pattern (backdrop, centered modal, Cancel + Confirm buttons). Use the same styling conventions as `edit-org-dialog.tsx`.

### 2. Add confirmation to Toggle Organization Status

**File:** `app/admin/organizations/page.tsx` (OrgRow component)

- Add state: `confirmStatusTarget: { orgId, currentStatus } | null`
- On status button click → set confirm target instead of calling mutation
- Show `ConfirmDialog` with message: "Are you sure you want to {activate/deactivate} {org.name}?"
- On confirm → call `handleToggleOrgStatus`, close dialog
- Use `confirmVariant: "danger"` for deactivation

### 3. Add confirmation to Toggle User Status

**File:** `components/admin/user-status-toggle.tsx`

- Add state: `showConfirm: boolean`
- On toggle button click → set `showConfirm = true`
- Show `ConfirmDialog` with message: "Are you sure you want to {activate/deactivate} {user.name} ({user.email})?"
- On confirm → call existing toggle logic, close dialog
- Use `confirmVariant: "danger"` for deactivation

---

## Out of Scope

- Change Password form submit (user is explicitly filling a form — that IS the confirmation)
- Dismiss/Logout button (low-risk, non-destructive, easily reversible)
- Edit Org / Transfer Ownership / Reset Password (already have form dialogs)
