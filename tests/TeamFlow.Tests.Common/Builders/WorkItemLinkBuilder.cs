using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class WorkItemLinkBuilder
{
    private Guid _sourceId = Guid.NewGuid();
    private Guid _targetId = Guid.NewGuid();
    private LinkType _linkType = LinkType.RelatesTo;
    private LinkScope _scope = LinkScope.SameProject;
    private Guid _createdById = Guid.NewGuid();

    public static WorkItemLinkBuilder New() => new();

    public WorkItemLinkBuilder WithSource(Guid sourceId) { _sourceId = sourceId; return this; }
    public WorkItemLinkBuilder WithTarget(Guid targetId) { _targetId = targetId; return this; }
    public WorkItemLinkBuilder WithLinkType(LinkType type) { _linkType = type; return this; }
    public WorkItemLinkBuilder WithScope(LinkScope scope) { _scope = scope; return this; }
    public WorkItemLinkBuilder WithCreatedBy(Guid createdById) { _createdById = createdById; return this; }
    public WorkItemLinkBuilder Blocks() { _linkType = LinkType.Blocks; return this; }
    public WorkItemLinkBuilder DependsOn() { _linkType = LinkType.DependsOn; return this; }
    public WorkItemLinkBuilder CrossProject() { _scope = LinkScope.CrossProject; return this; }

    public WorkItemLink Build() => new()
    {
        SourceId = _sourceId,
        TargetId = _targetId,
        LinkType = _linkType,
        Scope = _scope,
        CreatedById = _createdById
    };
}
