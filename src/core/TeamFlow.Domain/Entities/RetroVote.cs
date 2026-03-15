namespace TeamFlow.Domain.Entities;

public class RetroVote
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid CardId { get; set; }
    public Guid VoterId { get; set; }
    public short VoteCount { get; set; } = 1; // 1 or 2

    // Navigation
    public RetroCard? Card { get; set; }
    public User? Voter { get; set; }
}
