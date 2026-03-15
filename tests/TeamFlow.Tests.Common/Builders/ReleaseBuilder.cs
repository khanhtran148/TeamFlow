using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class ReleaseBuilder
{
    private Guid _projectId = Guid.NewGuid();
    private string _name = "v1.0.0";
    private string? _description;
    private DateOnly? _releaseDate;
    private ReleaseStatus _status = ReleaseStatus.Unreleased;
    private bool _notesLocked;

    public static ReleaseBuilder New() => new();

    public ReleaseBuilder WithProject(Guid projectId) { _projectId = projectId; return this; }
    public ReleaseBuilder WithName(string name) { _name = name; return this; }
    public ReleaseBuilder WithDescription(string description) { _description = description; return this; }
    public ReleaseBuilder WithReleaseDate(DateOnly date) { _releaseDate = date; return this; }
    public ReleaseBuilder WithStatus(ReleaseStatus status) { _status = status; return this; }
    public ReleaseBuilder WithNotesLocked(bool locked = true) { _notesLocked = locked; return this; }
    public ReleaseBuilder Released() { _status = ReleaseStatus.Released; return this; }

    public Release Build() => new()
    {
        ProjectId = _projectId,
        Name = _name,
        Description = _description,
        ReleaseDate = _releaseDate,
        Status = _status,
        NotesLocked = _notesLocked
    };
}
