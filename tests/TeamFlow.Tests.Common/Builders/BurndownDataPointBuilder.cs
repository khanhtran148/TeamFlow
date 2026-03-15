using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Common.Builders;

public sealed class BurndownDataPointBuilder
{
    private Guid _sprintId = Guid.NewGuid();
    private DateOnly _recordedDate = DateOnly.FromDateTime(DateTime.UtcNow);
    private int _remainingPoints = 10;
    private int _completedPoints;
    private int _addedPoints;
    private bool _isWeekend;

    public static BurndownDataPointBuilder New() => new();

    public BurndownDataPointBuilder WithSprint(Guid sprintId) { _sprintId = sprintId; return this; }
    public BurndownDataPointBuilder WithDate(DateOnly date) { _recordedDate = date; return this; }
    public BurndownDataPointBuilder WithRemainingPoints(int points) { _remainingPoints = points; return this; }
    public BurndownDataPointBuilder WithCompletedPoints(int points) { _completedPoints = points; return this; }
    public BurndownDataPointBuilder WithAddedPoints(int points) { _addedPoints = points; return this; }
    public BurndownDataPointBuilder OnWeekend() { _isWeekend = true; return this; }

    public BurndownDataPoint Build() => new()
    {
        SprintId = _sprintId,
        RecordedDate = _recordedDate,
        RemainingPoints = _remainingPoints,
        CompletedPoints = _completedPoints,
        AddedPoints = _addedPoints,
        IsWeekend = _isWeekend
    };
}
