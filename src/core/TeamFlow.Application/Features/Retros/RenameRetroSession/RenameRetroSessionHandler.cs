using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Retros.RenameRetroSession;

public sealed class RenameRetroSessionHandler(
    IRetroSessionRepository retroRepository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser)
    : IRequestHandler<RenameRetroSessionCommand, Result>
{
    public async Task<Result> Handle(RenameRetroSessionCommand request, CancellationToken ct)
    {
        var session = await retroRepository.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return Result.Failure("Retro session not found");

        if (!await permissionChecker.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return Result.Failure("Access denied");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure("Name is required");

        session.Name = request.Name.Trim();
        await retroRepository.UpdateAsync(session, ct);

        return Result.Success();
    }
}
