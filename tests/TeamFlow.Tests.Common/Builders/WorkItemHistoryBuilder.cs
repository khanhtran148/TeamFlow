using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Common.Builders;

public sealed class WorkItemHistoryBuilder
{
    private Guid _workItemId = Guid.NewGuid();
    private Guid? _actorId;
    private string _actorType = "User";
    private string _actionType = "Created";
    private string? _fieldName;
    private string? _oldValue;
    private string? _newValue;

    public static WorkItemHistoryBuilder New() => new();

    public WorkItemHistoryBuilder WithWorkItem(Guid workItemId) { _workItemId = workItemId; return this; }
    public WorkItemHistoryBuilder WithActor(Guid actorId) { _actorId = actorId; return this; }
    public WorkItemHistoryBuilder WithActorType(string actorType) { _actorType = actorType; return this; }
    public WorkItemHistoryBuilder WithAction(string actionType) { _actionType = actionType; return this; }
    public WorkItemHistoryBuilder WithField(string fieldName, string? oldValue, string? newValue)
    {
        _fieldName = fieldName;
        _oldValue = oldValue;
        _newValue = newValue;
        return this;
    }

    public WorkItemHistory Build() => new()
    {
        WorkItemId = _workItemId,
        ActorId = _actorId,
        ActorType = _actorType,
        ActionType = _actionType,
        FieldName = _fieldName,
        OldValue = _oldValue,
        NewValue = _newValue
    };
}
