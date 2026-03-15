using Asp.Versioning;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Common.Errors;

namespace TeamFlow.Api.Controllers.Base;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Maps a Result&lt;T&gt; (string-error variant) to the appropriate HTTP response.
    /// Uses semantic content-based matching — no fragile string prefix checks.
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        return MapStringError(result.Error);
    }

    /// <summary>
    /// Maps a Result (string-error variant) to the appropriate HTTP response.
    /// </summary>
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess) return Ok();
        return MapStringError(result.Error);
    }

    /// <summary>
    /// Maps a typed IDomainError to the appropriate HTTP response.
    /// Prefer this overload in new code — handlers should return typed errors.
    /// </summary>
    protected IActionResult HandleDomainError(IDomainError error) => error switch
    {
        NotFoundError e     => NotFound(CreateProblem(404, "Not Found", e.Message)),
        ForbiddenError e    => StatusCode(403, CreateProblem(403, "Forbidden", e.Message)),
        ValidationError e   => BadRequest(CreateProblem(400, "Bad Request", e.Message)),
        ConflictError e     => Conflict(CreateProblem(409, "Conflict", e.Message)),
        UnauthorizedError e => StatusCode(401, CreateProblem(401, "Unauthorized", e.Message)),
        _                   => StatusCode(500, CreateProblem(500, "Internal Error", "An unexpected error occurred."))
    };

    // ── Private helpers ───────────────────────────────────────────────────────

    private IActionResult MapStringError(string error)
    {
        if (IsNotFoundError(error))
            return NotFound(CreateProblem(404, "Not Found", error));

        if (IsForbiddenError(error))
            return StatusCode(403, CreateProblem(403, "Forbidden", error));

        if (IsConflictError(error))
            return Conflict(CreateProblem(409, "Conflict", error));

        return BadRequest(CreateProblem(400, "Bad Request", error));
    }

    // Centralised matching rules — no magic string prefixes scattered through switch arms.
    private static bool IsNotFoundError(string error) =>
        error.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private static bool IsForbiddenError(string error) =>
        error.Equals("Access denied", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("permission denied", StringComparison.OrdinalIgnoreCase);

    private static bool IsConflictError(string error) =>
        error.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("duplicate", StringComparison.OrdinalIgnoreCase);

    private ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Instance = HttpContext.Request.Path,
        Extensions =
        {
            ["correlationId"] = HttpContext.Items["X-Correlation-ID"]?.ToString()
        }
    };
}
