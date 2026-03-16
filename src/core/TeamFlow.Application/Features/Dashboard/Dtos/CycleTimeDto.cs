namespace TeamFlow.Application.Features.Dashboard.Dtos;

public sealed record CycleTimeDto(IReadOnlyList<CycleTimeByTypeDto> ByType);

public sealed record CycleTimeByTypeDto(
    string ItemType,
    double AvgDays,
    double MedianDays,
    double P90Days,
    int SampleSize
);
