using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros.GetRetroSession;

public sealed class GetRetroSessionHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetRetroSessionQuery, Result<RetroSessionDto>>
{
    public async Task<Result<RetroSessionDto>> Handle(GetRetroSessionQuery request, CancellationToken ct)
    {
        var session = await retroRepo.GetByIdWithDetailsAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<RetroSessionDto>("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_View, ct))
            return DomainError.Forbidden<RetroSessionDto>();

        var isAnonymous = session.AnonymityMode == RetroAnonymityModes.Anonymous;
        return Result.Success(RetroMapper.ToDto(session, isAnonymous));
    }
}
