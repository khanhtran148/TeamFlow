using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints.GetBurndown;

public sealed class GetBurndownHandler(
    ISprintRepository sprintRepository,
    IBurndownDataPointRepository burndownRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetBurndownQuery, Result<BurndownDto>>
{
    public async Task<Result<BurndownDto>> Handle(GetBurndownQuery request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdWithItemsAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure<BurndownDto>("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Project_View, ct))
            return Result.Failure<BurndownDto>("Access denied");

        var dataPoints = (await burndownRepository.GetBySprintAsync(sprint.Id, ct)).ToList();

        var actualLine = dataPoints.Select(dp => new ActualPointDto(
            dp.RecordedDate,
            dp.RemainingPoints,
            dp.CompletedPoints,
            dp.AddedPoints
        )).ToList();

        var idealLine = ComputeIdealLine(sprint);

        return Result.Success(new BurndownDto(sprint.Id, idealLine, actualLine));
    }

    private static List<IdealPointDto> ComputeIdealLine(Domain.Entities.Sprint sprint)
    {
        if (sprint.StartDate is null || sprint.EndDate is null)
            return [];

        var items = sprint.WorkItems?.ToList() ?? [];
        var totalPoints = (int)items.Sum(w => w.EstimationValue ?? 0);
        if (totalPoints == 0)
            return [];

        var start = sprint.StartDate.Value;
        var end = sprint.EndDate.Value;

        // Count working days (exclude weekends)
        var workingDays = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                workingDays.Add(d);
        }

        if (workingDays.Count == 0)
            return [new IdealPointDto(start, totalPoints)];

        var result = new List<IdealPointDto>();
        var pointsPerDay = (double)totalPoints / workingDays.Count;

        for (var i = 0; i < workingDays.Count; i++)
        {
            var remaining = (int)Math.Round(totalPoints - (pointsPerDay * (i + 1)));
            remaining = Math.Max(0, remaining);
            result.Add(new IdealPointDto(workingDays[i], remaining));
        }

        return result;
    }
}
