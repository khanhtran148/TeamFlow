using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Projects;

namespace TeamFlow.Application.Features.Projects.ListProjects;

public sealed record ListProjectsQuery(
    Guid? OrgId,
    string? Status,
    string? Search,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<ProjectDto>>>;
