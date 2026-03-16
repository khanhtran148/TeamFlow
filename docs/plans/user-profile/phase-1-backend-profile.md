# Phase 1: Backend -- GetProfile + UpdateProfile

**PARALLEL:** yes (with Phase 3: Frontend)
**Depends on:** API Contract (api-contract-260316-1600.md)
**TFD:** mandatory

---

## Summary

Add `AvatarUrl` to the User entity, create the GetProfile query and UpdateProfile command with full test coverage.

---

## FILE OWNERSHIP

This phase owns all files under:
- `src/core/TeamFlow.Domain/Entities/User.cs` (modify -- add AvatarUrl)
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs` (modify)
- `src/core/TeamFlow.Infrastructure/Migrations/` (new migration)
- `src/core/TeamFlow.Application/Features/Users/GetProfile/` (new folder)
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/` (new folder)
- `src/core/TeamFlow.Application/Features/Users/UserProfileDto.cs` (new)
- `src/apps/TeamFlow.Api/Controllers/UsersController.cs` (modify -- add endpoints)
- `tests/TeamFlow.Application.Tests/Features/Users/GetProfile/` (new)
- `tests/TeamFlow.Application.Tests/Features/Users/UpdateProfile/` (new)

---

## Tasks

### 1.1 Migration: Add avatar_url column

- Add `AvatarUrl` property to `User` entity (nullable string)
- Update `UserConfiguration`: map `avatar_url`, max length 2048, nullable
- Generate EF Core migration: `AddAvatarUrlToUser`

**Files:**
- `src/core/TeamFlow.Domain/Entities/User.cs`
- `src/core/TeamFlow.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/core/TeamFlow.Infrastructure/Migrations/<timestamp>_AddAvatarUrlToUser.cs`

### 1.2 DTOs

Create shared profile DTOs used by both GetProfile and UpdateProfile.

**Files:**
- `src/core/TeamFlow.Application/Features/Users/UserProfileDto.cs`

**DTOs (from contract):**
- `UserProfileDto`
- `ProfileOrganizationDto`
- `ProfileTeamDto`

### 1.3 GetProfile Query (TFD)

**Tests first** (`tests/TeamFlow.Application.Tests/Features/Users/GetProfile/GetProfileHandlerTests.cs`):
- `Handle_AuthenticatedUser_ReturnsFullProfile` -- returns user data with orgs and teams
- `Handle_UserNotFound_ReturnsFailure`
- `Handle_UserWithNoOrgsOrTeams_ReturnsEmptyCollections`

**Implementation:**
- `src/core/TeamFlow.Application/Features/Users/GetProfile/GetProfileQuery.cs`
- `src/core/TeamFlow.Application/Features/Users/GetProfile/GetProfileHandler.cs`

**Handler logic:**
1. Get user by `ICurrentUser.Id` from `IUserRepository`
2. If not found, return failure
3. Query org memberships with org details via `IOrganizationRepository` (or add method)
4. Query team memberships with team/project details (may need new repository method)
5. Map to `UserProfileDto` and return success

**Repository additions needed:**
- `IOrganizationRepository.ListMembershipsByUserAsync(Guid userId, CancellationToken ct)` -- returns org + role + joinedAt
- `ITeamRepository` or `ITeamMemberRepository` method to get team memberships by user with team name and project name

### 1.4 UpdateProfile Command (TFD)

**Tests first** (`tests/TeamFlow.Application.Tests/Features/Users/UpdateProfile/UpdateProfileHandlerTests.cs`):
- `Handle_ValidCommand_UpdatesNameAndAvatar`
- `Handle_ValidCommand_ReturnsUpdatedProfile`
- `Handle_NullAvatarUrl_ClearsAvatar`
- `Handle_UserNotFound_ReturnsFailure`

**Validator tests** (`tests/TeamFlow.Application.Tests/Features/Users/UpdateProfile/UpdateProfileValidatorTests.cs`):
- `[Theory]` with `[InlineData("")]`, `[InlineData(null)]` for empty name
- `Validate_NameTooLong_ReturnsError` (>100 chars)
- `Validate_AvatarUrlTooLong_ReturnsError` (>2048 chars)
- `Validate_ValidCommand_NoErrors`

**Implementation:**
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/UpdateProfileCommand.cs`
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/UpdateProfileValidator.cs`
- `src/core/TeamFlow.Application/Features/Users/UpdateProfile/UpdateProfileHandler.cs`

**Handler logic:**
1. Get user by `ICurrentUser.Id`
2. If not found, return failure
3. Update `user.Name` and `user.AvatarUrl`
4. Persist via `IUserRepository.UpdateAsync`
5. Fetch orgs and teams (same as GetProfile)
6. Return `UserProfileDto`

### 1.5 Controller Endpoints

Add to `UsersController`:

```csharp
[HttpGet("me/profile")]
[ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
public async Task<IActionResult> GetProfile(CancellationToken ct)

[HttpPut("me/profile")]
[EnableRateLimiting(RateLimitPolicies.Write)]
[ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> UpdateProfile(
    [FromBody] UpdateProfileCommand cmd, CancellationToken ct)
```

**Files:**
- `src/apps/TeamFlow.Api/Controllers/UsersController.cs`

---

## Acceptance Criteria

- [ ] `GET /api/v1/users/me/profile` returns full profile with orgs, teams, avatarUrl, systemRole, createdAt
- [ ] `PUT /api/v1/users/me/profile` updates name and avatarUrl; returns updated profile
- [ ] Validation rejects empty name, name > 100 chars, avatarUrl > 2048 chars
- [ ] All handler tests pass (TFD)
- [ ] Migration applies cleanly
