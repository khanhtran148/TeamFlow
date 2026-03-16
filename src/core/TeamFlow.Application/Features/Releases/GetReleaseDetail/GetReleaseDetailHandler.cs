using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Releases.GetReleaseDetail;

public sealed class GetReleaseDetailHandler(
    IReleaseRepository releaseRepo,
    IWorkItemRepository workItemRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetReleaseDetailQuery, Result<ReleaseDetailDto>>
{
    public async Task<Result<ReleaseDetailDto>> Handle(GetReleaseDetailQuery request, CancellationToken ct)
    {
        var release = await releaseRepo.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return DomainError.NotFound<ReleaseDetailDto>("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_View, ct))
            return DomainError.Forbidden<ReleaseDetailDto>();

        var items = await workItemRepo.GetByReleaseIdAsync(request.ReleaseId, ct);

        var itemList = items.ToList();

        var progress = new ReleaseProgressDto(
            TotalItems: itemList.Count,
            DoneItems: itemList.Count(i => i.Status == WorkItemStatus.Done),
            InProgressItems: itemList.Count(i => i.Status is WorkItemStatus.InProgress or WorkItemStatus.InReview),
            ToDoItems: itemList.Count(i => i.Status is WorkItemStatus.ToDo or WorkItemStatus.NeedsClarification),
            TotalPoints: itemList.Sum(i => i.EstimationValue ?? 0),
            DonePoints: itemList.Where(i => i.Status == WorkItemStatus.Done).Sum(i => i.EstimationValue ?? 0),
            InProgressPoints: itemList.Where(i => i.Status is WorkItemStatus.InProgress or WorkItemStatus.InReview).Sum(i => i.EstimationValue ?? 0),
            ToDoPoints: itemList.Where(i => i.Status is WorkItemStatus.ToDo or WorkItemStatus.NeedsClarification).Sum(i => i.EstimationValue ?? 0)
        );

        var byEpic = itemList
            .GroupBy(i => i.Parent?.Title ?? "Unparented")
            .Select(g => new ReleaseGroupDto(g.Key, g.First().ParentId, g.Count(), g.Count(i => i.Status == WorkItemStatus.Done)))
            .ToList();

        var byAssignee = itemList
            .GroupBy(i => i.Assignee?.Name ?? "Unassigned")
            .Select(g => new ReleaseGroupDto(g.Key, g.First().AssigneeId, g.Count(), g.Count(i => i.Status == WorkItemStatus.Done)))
            .ToList();

        var bySprint = itemList
            .GroupBy(i => i.Sprint?.Name ?? "No Sprint")
            .Select(g => new ReleaseGroupDto(g.Key, g.First().SprintId, g.Count(), g.Count(i => i.Status == WorkItemStatus.Done)))
            .ToList();

        var isOverdue = release.ReleaseDate.HasValue &&
                        release.ReleaseDate.Value < DateOnly.FromDateTime(DateTime.UtcNow) &&
                        release.Status != ReleaseStatus.Released;

        return Result.Success(new ReleaseDetailDto(
            release.Id,
            release.Name,
            release.Description,
            release.ReleaseNotes,
            release.ReleaseDate,
            release.Status,
            release.NotesLocked,
            isOverdue,
            progress,
            byEpic,
            byAssignee,
            bySprint,
            release.CreatedAt
        ));
    }
}
