using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Common.Behaviors;

public sealed class ActiveUserBehavior<TRequest, TResponse>(
    ICurrentUser currentUser,
    IUserRepository userRepository)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const string DeactivatedMessage = "Your account has been deactivated.";

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip for unauthenticated requests (login, register, refresh-token)
        if (!currentUser.IsAuthenticated)
            return await next(cancellationToken);

        var user = await userRepository.GetByIdAsync(currentUser.Id, cancellationToken);
        if (user is not null && !user.IsActive)
            return BuildForbiddenResponse();

        return await next(cancellationToken);
    }

    private static TResponse BuildForbiddenResponse()
    {
        var responseType = typeof(TResponse);

        // Result<T> case
        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "Failure" &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(string));

            if (failureMethod is not null)
            {
                var genericMethod = failureMethod.MakeGenericMethod(innerType);
                return (TResponse)genericMethod.Invoke(null, [DeactivatedMessage])!;
            }
        }

        // Result (non-generic) case
        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(DeactivatedMessage);

        // Fallback — should not reach here in practice
        throw new InvalidOperationException(
            $"ActiveUserBehavior cannot construct a failure response for type {responseType.Name}.");
    }
}
