using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Retros.StartRetroSession;

public sealed class StartRetroSessionHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<StartRetroSessionCommand, Result<RetroSessionDto>>
{
    public async Task<Result<RetroSessionDto>> Handle(StartRetroSessionCommand request, CancellationToken ct)
    {
        var session = await retroRepo.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<RetroSessionDto>("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return DomainError.Forbidden<RetroSessionDto>();

        if (session.FacilitatorId != currentUser.Id)
            return DomainError.Forbidden<RetroSessionDto>("Only the facilitator can start the session");

        if (session.Status != RetroSessionStatus.Draft)
            return DomainError.Validation<RetroSessionDto>("Session can only be started from Draft status");

        session.Status = RetroSessionStatus.Open;
        await retroRepo.UpdateAsync(session, ct);

        await publisher.Publish(new RetroSessionStartedDomainEvent(
            session.Id, session.ProjectId, session.SprintId, session.FacilitatorId), ct);

        return Result.Success(RetroMapper.ToDto(session, session.AnonymityMode == RetroAnonymityModes.Anonymous));
    }
}
