using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.AssignWorkItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class AssignWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public AssignWorkItemTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private AssignWorkItemHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidAssignment_SetsAssignee()
    {
        var item = WorkItemBuilder.New().WithType(WorkItemType.Task).Build();
        var assigneeId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UserExistsAsync(assigneeId, Arg.Any<CancellationToken>()).Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());

        var result = await CreateHandler().Handle(new AssignWorkItemCommand(item.Id, assigneeId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task Handle_AssignEpic_ReturnsValidationError()
    {
        var epic = WorkItemBuilder.New().AsEpic().Build();
        var assigneeId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(epic.Id, Arg.Any<CancellationToken>()).Returns(epic);
        _workItemRepo.UserExistsAsync(assigneeId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(new AssignWorkItemCommand(epic.Id, assigneeId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Epic");
    }

    [Fact]
    public async Task Handle_InvalidUser_ReturnsNotFound()
    {
        var item = WorkItemBuilder.New().WithType(WorkItemType.Task).Build();
        var assigneeId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UserExistsAsync(assigneeId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new AssignWorkItemCommand(item.Id, assigneeId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_Assignment_RecordsHistory()
    {
        var prevAssigneeId = Guid.NewGuid();
        var item = WorkItemBuilder.New().WithType(WorkItemType.Task).WithAssignee(prevAssigneeId).Build();
        var newAssigneeId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UserExistsAsync(newAssigneeId, Arg.Any<CancellationToken>()).Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());

        await CreateHandler().Handle(new AssignWorkItemCommand(item.Id, newAssigneeId), CancellationToken.None);

        await _historyService.Received(1).RecordAsync(
            Arg.Is<WorkItemHistoryEntry>(e => e.FieldName == "AssigneeId" && e.OldValue == prevAssigneeId.ToString()),
            Arg.Any<CancellationToken>());
    }
}
