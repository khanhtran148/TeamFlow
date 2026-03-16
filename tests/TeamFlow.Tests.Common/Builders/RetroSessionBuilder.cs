using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class RetroSessionBuilder
{
    private Guid _projectId = Guid.NewGuid();
    private Guid _facilitatorId = Guid.NewGuid();
    private Guid? _sprintId;
    private string _anonymityMode = RetroAnonymityModes.Public;
    private RetroSessionStatus _status = RetroSessionStatus.Draft;

    public static RetroSessionBuilder New() => new();

    public RetroSessionBuilder WithProject(Guid projectId) { _projectId = projectId; return this; }
    public RetroSessionBuilder WithFacilitator(Guid facilitatorId) { _facilitatorId = facilitatorId; return this; }
    public RetroSessionBuilder WithSprint(Guid sprintId) { _sprintId = sprintId; return this; }
    public RetroSessionBuilder Anonymous() { _anonymityMode = RetroAnonymityModes.Anonymous; return this; }
    public RetroSessionBuilder WithStatus(RetroSessionStatus status) { _status = status; return this; }

    public RetroSession Build() => new()
    {
        ProjectId = _projectId,
        FacilitatorId = _facilitatorId,
        SprintId = _sprintId,
        AnonymityMode = _anonymityMode,
        Status = _status
    };
}
