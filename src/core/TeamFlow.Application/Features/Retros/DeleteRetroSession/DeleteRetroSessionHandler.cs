using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Retros.DeleteRetroSession;

public sealed class DeleteRetroSessionHandler(
    IRetroSessionRepository retroRepository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteRetroSessionCommand, Result>
{
    public async Task<Result> Handle(DeleteRetroSessionCommand request, CancellationToken ct)
    {
        var session = await retroRepository.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return Result.Failure("Retro session not found");

        if (!await permissionChecker.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return Result.Failure("Access denied");

        await retroRepository.DeleteAsync(session, ct);

        return Result.Success();
    }
}
