namespace TeamFlow.Domain.Entities;

public sealed class PlanningPokerSession
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid WorkItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid FacilitatorId { get; set; }
    public bool IsRevealed { get; set; }
    public decimal? FinalEstimate { get; set; }
    public Guid? ConfirmedById { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    // Navigation
    public WorkItem? WorkItem { get; set; }
    public Project? Project { get; set; }
    public User? Facilitator { get; set; }
    public User? ConfirmedBy { get; set; }
    public ICollection<PlanningPokerVote> Votes { get; set; } = [];
}
