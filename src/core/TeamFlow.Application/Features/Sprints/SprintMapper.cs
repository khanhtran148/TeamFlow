using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints;

internal static class SprintMapper
{
    public static SprintDto ToDto(Sprint sprint)
    {
        var items = sprint.WorkItems ?? [];
        var totalPoints = (int)items.Sum(w => w.EstimationValue ?? 0);
        var completedPoints = (int)items
            .Where(w => w.Status is WorkItemStatus.Done)
            .Sum(w => w.EstimationValue ?? 0);

        return new SprintDto(
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
            sprint.CreatedAt
        );
    }
}
