using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Admin.ChangeOrgStatus;
using TeamFlow.Application.Features.Admin.ChangeUserStatus;
using TeamFlow.Application.Features.Admin.ListOrganizations;
using TeamFlow.Application.Features.Admin.ListUsers;
using TeamFlow.Application.Features.Admin.ResetUserPassword;
using TeamFlow.Application.Features.Admin.TransferOrgOwnership;
using TeamFlow.Application.Features.Admin.UpdateOrganization;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
[Microsoft.AspNetCore.Authorization.Authorize]
[EnableRateLimiting("general")]
public sealed class AdminController : ApiControllerBase
{
    // ── GET /admin/organizations ───────────────────────────────────────────
    [HttpGet("organizations")]
    [ProducesResponseType(typeof(PagedResult<AdminOrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListOrganizations(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new AdminListOrganizationsQuery(search, page, pageSize), ct);
        return HandleResult(result);
    }

    // ── GET /admin/users ───────────────────────────────────────────────────
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Sender.Send(new AdminListUsersQuery(search, page, pageSize), ct);
        return HandleResult(result);
    }

    // ── POST /admin/users/{userId}/reset-password ──────────────────────────
    [HttpPost("users/{userId:guid}/reset-password")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetUserPassword(
        Guid userId,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken ct)
    {
        var result = await Sender.Send(new AdminResetUserPasswordCommand(userId, request.NewPassword), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ── PUT /admin/users/{userId}/status ──────────────────────────────────
    [HttpPut("users/{userId:guid}/status")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserStatus(
        Guid userId,
        [FromBody] ChangeStatusRequest request,
        CancellationToken ct)
    {
        var result = await Sender.Send(new AdminChangeUserStatusCommand(userId, request.IsActive), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ── PUT /admin/organizations/{orgId}/status ────────────────────────────
    [HttpPut("organizations/{orgId:guid}/status")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeOrgStatus(
        Guid orgId,
        [FromBody] ChangeStatusRequest request,
        CancellationToken ct)
    {
        var result = await Sender.Send(new AdminChangeOrgStatusCommand(orgId, request.IsActive), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ── PUT /admin/organizations/{orgId} ──────────────────────────────────
    [HttpPut("organizations/{orgId:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOrganization(
        Guid orgId,
        [FromBody] AdminUpdateOrgRequest request,
        CancellationToken ct)
    {
        var result = await Sender.Send(new AdminUpdateOrgCommand(orgId, request.Name, request.Slug), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ── PUT /admin/organizations/{orgId}/owner ─────────────────────────────
    [HttpPut("organizations/{orgId:guid}/owner")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferOrgOwnership(
        Guid orgId,
        [FromBody] TransferOwnershipRequest request,
        CancellationToken ct)
    {
        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(orgId, request.NewOwnerUserId), ct);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }
}

// ── Request body DTOs ──────────────────────────────────────────────────────

public sealed record ResetUserPasswordRequest(string NewPassword);
public sealed record ChangeStatusRequest(bool IsActive);
public sealed record AdminUpdateOrgRequest(string Name, string Slug);
public sealed record TransferOwnershipRequest(Guid NewOwnerUserId);
