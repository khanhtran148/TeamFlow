namespace TeamFlow.Application.Features.Dashboard.Dtos;

public sealed record ReleaseProgressDto(
    int DoneCount,
    int InProgressCount,
    int TodoCount,
    decimal DonePoints,
    decimal TotalPoints,
    double CompletionPct
);
