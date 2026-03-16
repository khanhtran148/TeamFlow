using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Admin.TransferOrgOwnership;

public sealed record AdminTransferOrgOwnershipCommand(
    Guid OrgId,
    Guid NewOwnerUserId
) : IRequest<Result>;
