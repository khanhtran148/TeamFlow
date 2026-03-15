using System.Text.Json;
using CSharpFunctionalExtensions;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class Sprint
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Planning;
    public JsonDocument? CapacityJson { get; set; } // {member_id: points}
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
    public ICollection<WorkItem> WorkItems { get; set; } = [];
    public ICollection<SprintSnapshot> Snapshots { get; set; } = [];
    public ICollection<BurndownDataPoint> BurndownDataPoints { get; set; } = [];

    // Domain methods

    /// <summary>
    /// Starts the sprint. Only Planning sprints with items and dates can be started.
    /// </summary>
    public Result Start()
    {
        if (Status != SprintStatus.Planning)
            return Result.Failure("Only a Planning sprint can be started");

        if (StartDate is null || EndDate is null)
            return Result.Failure("Sprint must have start and end dates before starting");

        if (WorkItems.Count == 0)
            return Result.Failure("Sprint must have at least one item before starting");

        Status = SprintStatus.Active;
        return Result.Success();
    }

    /// <summary>
    /// Completes the sprint. Only Active sprints can be completed.
    /// Returns the list of incomplete work item IDs that should be carried over.
    /// </summary>
    public Result<IReadOnlyList<Guid>> Complete()
    {
        if (Status != SprintStatus.Active)
            return Result.Failure<IReadOnlyList<Guid>>("Only an Active sprint can be completed");

        Status = SprintStatus.Completed;

        var incompleteItemIds = WorkItems
            .Where(w => w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Rejected)
            .Select(w => w.Id)
            .ToList();

        return Result.Success<IReadOnlyList<Guid>>(incompleteItemIds);
    }

    /// <summary>
    /// Checks if an item can be added to this sprint.
    /// Items can be added to Planning sprints freely.
    /// Items can only be added to Active sprints with elevated permissions (checked externally).
    /// Items cannot be added to Completed sprints.
    /// </summary>
    public bool CanAddItem() => Status is SprintStatus.Planning or SprintStatus.Active;
}
