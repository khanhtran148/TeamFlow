using Asp.Versioning;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Application.Common.Errors;

namespace TeamFlow.Api.Controllers.Base;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);

        return result.Error switch
        {
            string error when error.StartsWith("NotFound") =>
                NotFound(CreateProblem(404, "Not Found", error)),
            string error when error.StartsWith("Forbidden") =>
                StatusCode(403, CreateProblem(403, "Forbidden", error)),
            string error when error.StartsWith("Conflict") =>
                Conflict(CreateProblem(409, "Conflict", error)),
            string error =>
                BadRequest(CreateProblem(400, "Bad Request", error))
        };
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess) return Ok();

        return result.Error switch
        {
            string error when error.StartsWith("NotFound") =>
                NotFound(CreateProblem(404, "Not Found", error)),
            string error when error.StartsWith("Forbidden") =>
                StatusCode(403, CreateProblem(403, "Forbidden", error)),
            string error when error.StartsWith("Conflict") =>
                Conflict(CreateProblem(409, "Conflict", error)),
            string error =>
                BadRequest(CreateProblem(400, "Bad Request", error))
        };
    }

    /// <summary>
    /// Overload accepting typed domain errors from IDomainError.
    /// </summary>
    protected IActionResult HandleDomainError(IDomainError error) => error switch
    {
        NotFoundError e  => NotFound(CreateProblem(404, "Not Found", e.Message)),
        ForbiddenError e => StatusCode(403, CreateProblem(403, "Forbidden", e.Message)),
        ValidationError e => BadRequest(CreateProblem(400, "Bad Request", e.Message)),
        ConflictError e  => Conflict(CreateProblem(409, "Conflict", e.Message)),
        _                => StatusCode(500, CreateProblem(500, "Internal Error", "An unexpected error occurred."))
    };

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
