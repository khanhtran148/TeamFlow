using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class WorkItemBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private Guid _projectId = Guid.NewGuid();
    private Guid? _parentId;
    private WorkItemType _type = WorkItemType.UserStory;
    private string _title = F.Lorem.Sentence(4);
    private string? _description;
    private WorkItemStatus _status = WorkItemStatus.ToDo;
    private Priority _priority = Priority.Medium;
    private Guid? _assigneeId;
    private Guid? _sprintId;
    private Guid? _releaseId;
    private decimal? _estimationValue;

    public static WorkItemBuilder New() => new();

    public WorkItemBuilder WithProject(Guid projectId) { _projectId = projectId; return this; }
    public WorkItemBuilder WithParent(Guid parentId) { _parentId = parentId; return this; }
    public WorkItemBuilder WithType(WorkItemType type) { _type = type; return this; }
    public WorkItemBuilder WithTitle(string title) { _title = title; return this; }
    public WorkItemBuilder WithDescription(string description) { _description = description; return this; }
    public WorkItemBuilder WithStatus(WorkItemStatus status) { _status = status; return this; }
    public WorkItemBuilder WithPriority(Priority priority) { _priority = priority; return this; }
    public WorkItemBuilder WithAssignee(Guid assigneeId) { _assigneeId = assigneeId; return this; }
    public WorkItemBuilder WithSprint(Guid sprintId) { _sprintId = sprintId; return this; }
    public WorkItemBuilder WithRelease(Guid releaseId) { _releaseId = releaseId; return this; }
    public WorkItemBuilder WithEstimation(decimal points) { _estimationValue = points; return this; }
    public WorkItemBuilder AsEpic() { _type = WorkItemType.Epic; _parentId = null; return this; }
    public WorkItemBuilder AsTask() { _type = WorkItemType.Task; return this; }
    public WorkItemBuilder AsBug() { _type = WorkItemType.Bug; return this; }

    public WorkItem Build() => new()
    {
        ProjectId = _projectId,
        ParentId = _parentId,
        Type = _type,
        Title = _title,
        Description = _description,
        Status = _status,
        Priority = _priority,
        AssigneeId = _assigneeId,
        SprintId = _sprintId,
        ReleaseId = _releaseId,
        EstimationValue = _estimationValue
    };
}
