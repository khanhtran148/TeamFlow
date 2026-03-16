using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.GetWorkItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class GetWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public GetWorkItemTests()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetWorkItemHandler CreateHandler() => new(_workItemRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ExistingItem_ReturnsDto()
    {
        var item = WorkItemBuilder.New().WithTitle("Test Item").WithType(WorkItemType.Task).Build();
        _workItemRepo.GetByIdWithDetailsAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(new GetWorkItemQuery(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Item");
        result.Value.Type.Should().Be(WorkItemType.Task);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var itemId = Guid.NewGuid();
        _workItemRepo.GetByIdWithDetailsAsync(itemId, Arg.Any<CancellationToken>()).Returns((WorkItem?)null);

        var result = await CreateHandler().Handle(new GetWorkItemQuery(itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AssignedItem_ReturnsAssignedAtInDto()
    {
        var assignedAt = new DateTime(2026, 3, 15, 9, 23, 11, DateTimeKind.Utc);
        var item = WorkItemBuilder.New()
            .WithType(WorkItemType.Task)
            .WithAssignee(Guid.NewGuid())
            .WithAssignedAt(assignedAt)
            .Build();
        _workItemRepo.GetByIdWithDetailsAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(new GetWorkItemQuery(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AssignedAt.Should().Be(assignedAt);
    }

    [Fact]
    public async Task Handle_UnassignedItem_ReturnsNullAssignedAtInDto()
    {
        var item = WorkItemBuilder.New()
            .WithType(WorkItemType.Task)
            .Build();
        _workItemRepo.GetByIdWithDetailsAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(new GetWorkItemQuery(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AssignedAt.Should().BeNull();
    }
}
