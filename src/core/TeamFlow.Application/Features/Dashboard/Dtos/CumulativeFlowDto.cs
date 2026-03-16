namespace TeamFlow.Application.Features.Dashboard.Dtos;

public sealed record CumulativeFlowDto(IReadOnlyList<CumulativeFlowPointDto> DataPoints);

public sealed record CumulativeFlowPointDto(
    DateOnly Date,
    int ToDo,
    int InProgress,
    int InReview,
    int Done
);
