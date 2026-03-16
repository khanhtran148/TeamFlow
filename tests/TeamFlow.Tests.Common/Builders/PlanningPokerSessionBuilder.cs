using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Common.Builders;

public sealed class PlanningPokerSessionBuilder
{
    private Guid _workItemId = Guid.NewGuid();
    private Guid _projectId = Guid.NewGuid();
    private Guid _facilitatorId = Guid.NewGuid();
    private bool _isRevealed;
    private decimal? _finalEstimate;
    private Guid? _confirmedById;
    private DateTime? _closedAt;

    public static PlanningPokerSessionBuilder New() => new();

    public PlanningPokerSessionBuilder WithWorkItem(Guid workItemId) { _workItemId = workItemId; return this; }
    public PlanningPokerSessionBuilder WithProject(Guid projectId) { _projectId = projectId; return this; }
    public PlanningPokerSessionBuilder WithFacilitator(Guid facilitatorId) { _facilitatorId = facilitatorId; return this; }
    public PlanningPokerSessionBuilder Revealed() { _isRevealed = true; return this; }
    public PlanningPokerSessionBuilder WithFinalEstimate(decimal estimate, Guid confirmedById)
    {
        _finalEstimate = estimate;
        _confirmedById = confirmedById;
        return this;
    }
    public PlanningPokerSessionBuilder Closed() { _closedAt = DateTime.UtcNow; return this; }

    public PlanningPokerSession Build() => new()
    {
        WorkItemId = _workItemId,
        ProjectId = _projectId,
        FacilitatorId = _facilitatorId,
        IsRevealed = _isRevealed,
        FinalEstimate = _finalEstimate,
        ConfirmedById = _confirmedById,
        ClosedAt = _closedAt
    };
}
