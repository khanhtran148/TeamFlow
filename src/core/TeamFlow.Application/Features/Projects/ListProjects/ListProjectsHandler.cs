using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Projects;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Projects.ListProjects;

public sealed class ListProjectsHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListProjectsQuery, Result<PagedResult<ProjectDto>>>
{
    public async Task<Result<PagedResult<ProjectDto>>> Handle(ListProjectsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await projectRepository.ListAsync(
            currentUser.Id,
            request.OrgId,
            request.Status,
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        var dtos = items.Select(MapToDto);

        return Result.Success(new PagedResult<ProjectDto>(dtos, totalCount, request.Page, request.PageSize));
    }

    private static ProjectDto MapToDto(Project project) =>
        new(
            project.Id,
            project.OrgId,
            project.Name,
            project.Description,
            project.Status,
            0,
            0,
            project.CreatedAt,
            project.UpdatedAt
        );
}
