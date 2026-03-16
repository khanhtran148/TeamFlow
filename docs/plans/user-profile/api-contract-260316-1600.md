# API Contract: User Profile Management

**Date:** 2026-03-16
**Status:** Draft

---

## Endpoints

### 1. GET /api/v1/users/me/profile

Returns the full profile for the authenticated user.

**Auth:** Bearer token required

**Response 200:**
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "name": "Jane Doe",
  "avatarUrl": "https://example.com/avatar.jpg",
  "systemRole": "User",
  "createdAt": "2026-03-15T09:23:11Z",
  "organizations": [
    {
      "orgId": "uuid",
      "orgName": "Acme Corp",
      "orgSlug": "acme-corp",
      "role": "Admin",
      "joinedAt": "2026-03-15T09:23:11Z"
    }
  ],
  "teams": [
    {
      "teamId": "uuid",
      "teamName": "Backend Squad",
      "projectId": "uuid",
      "projectName": "TeamFlow",
      "role": "Developer",
      "joinedAt": "2026-03-15T09:23:11Z"
    }
  ]
}
```

**Response DTOs (C#):**
```csharp
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string SystemRole,
    DateTime CreatedAt,
    IReadOnlyList<ProfileOrganizationDto> Organizations,
    IReadOnlyList<ProfileTeamDto> Teams
);

public sealed record ProfileOrganizationDto(
    Guid OrgId,
    string OrgName,
    string OrgSlug,
    string Role,
    DateTime JoinedAt
);

public sealed record ProfileTeamDto(
    Guid TeamId,
    string TeamName,
    Guid ProjectId,
    string ProjectName,
    string Role,
    DateTime JoinedAt
);
```

**Error responses:** 401 Unauthorized

---

### 2. PUT /api/v1/users/me/profile

Updates the authenticated user's profile (name, avatarUrl).

**Auth:** Bearer token required

**Request body:**
```json
{
  "name": "Jane Smith",
  "avatarUrl": "https://example.com/new-avatar.jpg"
}
```

**Validation rules:**
- `name`: required, 1-100 characters
- `avatarUrl`: optional, max 2048 characters, must be valid URL format when provided

**Response 200:**
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "name": "Jane Smith",
  "avatarUrl": "https://example.com/new-avatar.jpg",
  "systemRole": "User",
  "createdAt": "2026-03-15T09:23:11Z",
  "organizations": [...],
  "teams": [...]
}
```

**Command (C#):**
```csharp
public sealed record UpdateProfileCommand(
    string Name,
    string? AvatarUrl
) : IRequest<Result<UserProfileDto>>;
```

**Error responses:**
- 400 Bad Request (validation errors)
- 401 Unauthorized

---

### 3. GET /api/v1/users/me/activity

Returns a paginated list of the authenticated user's recent actions.

**Auth:** Bearer token required

**Query params:**
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 50)

**Response 200:**
```json
{
  "items": [
    {
      "id": "uuid",
      "workItemId": "uuid",
      "workItemTitle": "Implement login page",
      "actionType": "StatusChanged",
      "fieldName": "Status",
      "oldValue": "ToDo",
      "newValue": "InProgress",
      "createdAt": "2026-03-15T09:23:11Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Response DTO (C#):**
```csharp
public sealed record ActivityLogItemDto(
    Guid Id,
    Guid WorkItemId,
    string WorkItemTitle,
    string ActionType,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt
);
```

**Error responses:** 401 Unauthorized

---

## Frontend TypeScript Types

```typescript
// Profile DTOs
export interface UserProfileDto {
  id: string;
  email: string;
  name: string;
  avatarUrl: string | null;
  systemRole: SystemRole;
  createdAt: string;
  organizations: ProfileOrganizationDto[];
  teams: ProfileTeamDto[];
}

export interface ProfileOrganizationDto {
  orgId: string;
  orgName: string;
  orgSlug: string;
  role: OrgRole;
  joinedAt: string;
}

export interface ProfileTeamDto {
  teamId: string;
  teamName: string;
  projectId: string;
  projectName: string;
  role: string;
  joinedAt: string;
}

export interface UpdateProfileBody {
  name: string;
  avatarUrl?: string | null;
}

// Activity DTOs
export interface ActivityLogItemDto {
  id: string;
  workItemId: string;
  workItemTitle: string;
  actionType: string;
  fieldName: string | null;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}
```

---

## Existing Endpoints (reused, not modified)

- `POST /api/v1/auth/change-password` -- already implemented
- `GET /api/v1/notifications/preferences` -- already implemented
- `PUT /api/v1/notifications/preferences` -- already implemented
