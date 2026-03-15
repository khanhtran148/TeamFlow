using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using TeamFlow.Application.Common.Errors;

namespace TeamFlow.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Try to return a Result failure if TResponse is a Result type
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType)
        {
            var genericTypeDef = responseType.GetGenericTypeDefinition();
            if (genericTypeDef == typeof(Result<>))
            {
                var innerType = responseType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethods()
                    .FirstOrDefault(m => m.Name == "Failure" && m.IsGenericMethod && m.GetParameters().Length == 1);

                if (failureMethod is not null)
                {
                    var genericMethod = failureMethod.MakeGenericMethod(innerType);
                    var result = genericMethod.Invoke(null, [errorMessage]);
                    return (TResponse)result!;
                }
            }
        }

        if (responseType == typeof(Result))
        {
            var result = Result.Failure(errorMessage);
            return (TResponse)(object)result;
        }

        throw new ValidationException(failures);
    }
}
