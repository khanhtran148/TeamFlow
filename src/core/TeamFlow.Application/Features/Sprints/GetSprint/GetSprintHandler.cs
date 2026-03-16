using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints.GetSprint;

public sealed class GetSprintHandler(
    ISprintRepository sprintRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetSprintQuery, Result<SprintDetailDto>>
{
    public async Task<Result<SprintDetailDto>> Handle(GetSprintQuery request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdWithItemsAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure<SprintDetailDto>("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Project_View, ct))
            return Result.Failure<SprintDetailDto>("Access denied");

        var items = sprint.WorkItems?.ToList() ?? [];
        var totalPoints = (int)items.Sum(w => w.EstimationValue ?? 0);
        var completedPoints = (int)items
            .Where(w => w.Status is WorkItemStatus.Done)
            .Sum(w => w.EstimationValue ?? 0);

        var workItemDtos = items.Select(w => new WorkItemDto(
            w.Id, w.ProjectId, w.ParentId, w.Type, w.Title, w.Description,
            w.Status, w.Priority, w.EstimationValue, w.AssigneeId,
            w.Assignee?.Name, w.AssignedAt, w.SprintId, w.ReleaseId, 0, 0, w.SortOrder,
            w.CreatedAt, w.UpdatedAt
        )).ToList();

        var capacityEntries = ParseCapacity(sprint.CapacityJson, items);

        return Result.Success(new SprintDetailDto(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.Status,
            totalPoints,
            completedPoints,
            items.Count,
            null,
            sprint.CreatedAt,
            workItemDtos,
            capacityEntries
        ));
    }

    private static List<CapacityEntryDto> ParseCapacity(
        JsonDocument? capacityJson,
        List<Domain.Entities.WorkItem> items)
    {
        if (capacityJson is null)
            return [];

        var result = new List<CapacityEntryDto>();
        foreach (var prop in capacityJson.RootElement.EnumerateObject())
        {
            if (!Guid.TryParse(prop.Name, out var memberId))
                continue;

            var capacityPoints = prop.Value.GetInt32();
            var assignedPoints = (int)items
                .Where(w => w.AssigneeId == memberId)
                .Sum(w => w.EstimationValue ?? 0);

            result.Add(new CapacityEntryDto(memberId, string.Empty, capacityPoints, assignedPoints));
        }

        return result;
    }
}
