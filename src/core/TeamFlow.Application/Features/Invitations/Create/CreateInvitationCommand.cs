using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations.Create;

public sealed record CreateInvitationCommand(
    Guid OrgId,
    string? Email,
    OrgRole Role
) : IRequest<Result<CreateInvitationResponse>>;
