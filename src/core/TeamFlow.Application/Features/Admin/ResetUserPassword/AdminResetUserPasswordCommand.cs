using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.ResetUserPassword;

public sealed record AdminResetUserPasswordCommand(
    Guid UserId,
    string NewPassword
) : IRequest<Result>;
