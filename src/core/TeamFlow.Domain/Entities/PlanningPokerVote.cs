namespace TeamFlow.Domain.Entities;

public sealed class PlanningPokerVote
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid VoterId { get; set; }
    public decimal Value { get; set; }
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PlanningPokerSession? Session { get; set; }
    public User? Voter { get; set; }
}
