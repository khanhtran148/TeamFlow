using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Retros.UpdateColumnsConfig;

public sealed class UpdateColumnsConfigHandler(
    IRetroSessionRepository retroRepository,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateColumnsConfigCommand, Result>
{
    public async Task<Result> Handle(UpdateColumnsConfigCommand request, CancellationToken ct)
    {
        var session = await retroRepository.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return Result.Failure("Retro session not found");

        if (!await permissionChecker.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return Result.Failure("Access denied");

        session.ColumnsConfig = request.ColumnsConfig;
        await retroRepository.UpdateAsync(session, ct);

        return Result.Success();
    }
}
