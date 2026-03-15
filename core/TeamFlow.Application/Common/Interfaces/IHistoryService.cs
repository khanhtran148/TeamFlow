using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IHistoryService
{
    /// <summary>
    /// Records a work item history entry (append-only — never updates or deletes).
    /// </summary>
    Task RecordAsync(WorkItemHistoryEntry entry, CancellationToken ct = default);
}

public record WorkItemHistoryEntry(
    Guid WorkItemId,
    Guid? ActorId,
    string ActionType,
    string? FieldName = null,
    string? OldValue = null,
    string? NewValue = null,
    string ActorType = "User"
);
