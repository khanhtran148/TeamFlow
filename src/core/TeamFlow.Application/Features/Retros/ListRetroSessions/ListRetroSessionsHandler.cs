using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Retros.ListRetroSessions;

public sealed class ListRetroSessionsHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<ListRetroSessionsQuery, Result<ListRetroSessionsResponse>>
{
    public async Task<Result<ListRetroSessionsResponse>> Handle(ListRetroSessionsQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Retro_View, ct))
            return DomainError.Forbidden<ListRetroSessionsResponse>();

        var (items, totalCount) = await retroRepo.ListByProjectAsync(
            request.ProjectId, request.Page, request.PageSize, ct);

        var dtos = items.Select(RetroMapper.ToSummaryDto).ToList();

        return Result.Success(new ListRetroSessionsResponse(dtos, totalCount, request.Page, request.PageSize));
    }
}
