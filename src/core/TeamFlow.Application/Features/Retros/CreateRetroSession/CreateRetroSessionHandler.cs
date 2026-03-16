using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Retros.CreateRetroSession;

public sealed class CreateRetroSessionHandler(
    IRetroSessionRepository retroRepo,
    IProjectRepository projectRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<CreateRetroSessionCommand, Result<RetroSessionDto>>
{
    public async Task<Result<RetroSessionDto>> Handle(CreateRetroSessionCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Retro_Facilitate, ct))
            return DomainError.Forbidden<RetroSessionDto>();

        if (!await projectRepo.ExistsAsync(request.ProjectId, ct))
            return DomainError.NotFound<RetroSessionDto>("Project not found");

        var session = new RetroSession
        {
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Retro" : request.Name.Trim(),
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            FacilitatorId = currentUser.Id,
            AnonymityMode = request.AnonymityMode
        };

        await retroRepo.AddAsync(session, ct);

        return Result.Success(RetroMapper.ToDto(session, isAnonymous: false));
    }
}
