namespace TeamFlow.Domain.Entities;

public class BurndownDataPoint
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SprintId { get; set; }
    public DateOnly RecordedDate { get; set; }
    public int RemainingPoints { get; set; }
    public int CompletedPoints { get; set; }
    public int AddedPoints { get; set; } = 0;
    public bool IsWeekend { get; set; } = false;
    public DateTime RecordedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Sprint? Sprint { get; set; }
}
