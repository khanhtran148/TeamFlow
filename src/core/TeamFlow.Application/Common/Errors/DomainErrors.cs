using CSharpFunctionalExtensions;

namespace TeamFlow.Application.Common.Errors;

public record NotFoundError(string Message = "Resource not found") : IDomainError;
public record ForbiddenError(string Message = "Access denied") : IDomainError;
public record ValidationError(string Message = "Validation failed") : IDomainError;
public record ConflictError(string Message = "Conflict") : IDomainError;
public record UnauthorizedError(string Message = "Unauthorized") : IDomainError;

public interface IDomainError
{
    string Message { get; }
}

/// <summary>
/// Bridges typed <see cref="IDomainError"/> to <see cref="Result{T}"/> string-error failures.
/// Encodes the error type prefix so <see cref="TeamFlow.Api.Controllers.Base.ApiControllerBase"/>
/// can map to the correct HTTP status code.
/// </summary>
public static class DomainError
{
    public static Result<T> NotFound<T>(string message = "Resource not found")
        => Result.Failure<T>(new NotFoundError(message).Message);

    public static Result NotFound(string message = "Resource not found")
        => Result.Failure(new NotFoundError(message).Message);

    public static Result<T> Forbidden<T>(string message = "Access denied")
        => Result.Failure<T>(new ForbiddenError(message).Message);

    public static Result Forbidden(string message = "Access denied")
        => Result.Failure(new ForbiddenError(message).Message);

    public static Result<T> Conflict<T>(string message = "Conflict")
        => Result.Failure<T>(new ConflictError(message).Message);

    public static Result Conflict(string message = "Conflict")
        => Result.Failure(new ConflictError(message).Message);

    public static Result<T> Validation<T>(string message = "Validation failed")
        => Result.Failure<T>(new ValidationError(message).Message);

    public static Result Validation(string message = "Validation failed")
        => Result.Failure(new ValidationError(message).Message);
}
