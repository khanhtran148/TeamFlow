using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros.TransitionRetroSession;

public sealed class TransitionRetroSessionHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<TransitionRetroSessionCommand, Result<RetroSessionDto>>
{
    private static readonly Dictionary<RetroSessionStatus, RetroSessionStatus> ValidTransitions = new()
    {
        [RetroSessionStatus.Open] = RetroSessionStatus.Voting,
        [RetroSessionStatus.Voting] = RetroSessionStatus.Discussing,
    };

    public async Task<Result<RetroSessionDto>> Handle(TransitionRetroSessionCommand request, CancellationToken ct)
    {
        var session = await retroRepo.GetByIdWithDetailsAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<RetroSessionDto>("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return DomainError.Forbidden<RetroSessionDto>();

        if (session.FacilitatorId != currentUser.Id)
            return DomainError.Forbidden<RetroSessionDto>("Only the facilitator can transition the session");

        if (!ValidTransitions.TryGetValue(session.Status, out var expectedTarget) || expectedTarget != request.TargetStatus)
            return DomainError.Validation<RetroSessionDto>($"Invalid transition from {session.Status} to {request.TargetStatus}");

        session.Status = request.TargetStatus;
        await retroRepo.UpdateAsync(session, ct);

        return Result.Success(RetroMapper.ToDto(session, session.AnonymityMode == RetroAnonymityModes.Anonymous));
    }
}
