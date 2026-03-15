using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class SprintBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private const int MinSprintNumber = 1;
    private const int MaxSprintNumber = 99;

    private Guid _projectId = Guid.NewGuid();
    private string _name = $"Sprint {F.Random.Number(MinSprintNumber, MaxSprintNumber)}";
    private string? _goal;
    private DateOnly? _startDate;
    private DateOnly? _endDate;
    private SprintStatus _status = SprintStatus.Planning;

    public static SprintBuilder New() => new();

    public SprintBuilder WithProject(Guid projectId) { _projectId = projectId; return this; }
    public SprintBuilder WithName(string name) { _name = name; return this; }
    public SprintBuilder WithGoal(string goal) { _goal = goal; return this; }
    public SprintBuilder WithDates(DateOnly start, DateOnly end) { _startDate = start; _endDate = end; return this; }
    public SprintBuilder WithStatus(SprintStatus status) { _status = status; return this; }
    public SprintBuilder Active() { _status = SprintStatus.Active; return this; }
    public SprintBuilder Completed() { _status = SprintStatus.Completed; return this; }

    public Sprint Build() => new()
    {
        ProjectId = _projectId,
        Name = _name,
        Goal = _goal,
        StartDate = _startDate,
        EndDate = _endDate,
        Status = _status
    };
}
