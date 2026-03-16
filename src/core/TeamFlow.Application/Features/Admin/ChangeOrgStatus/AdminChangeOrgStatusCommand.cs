using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.ChangeOrgStatus;

public sealed record AdminChangeOrgStatusCommand(
    Guid OrgId,
    bool IsActive
) : IRequest<Result>;
