using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.ChangeUserStatus;

public sealed record AdminChangeUserStatusCommand(
    Guid UserId,
    bool IsActive
) : IRequest<Result>;
