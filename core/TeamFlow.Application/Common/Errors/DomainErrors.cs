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
