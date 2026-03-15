using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.ChangeStatus;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class ChangeStatusTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public ChangeStatusTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ChangeWorkItemStatusHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Theory]
    [InlineData(WorkItemStatus.ToDo, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.ToDo)]
    public async Task Handle_ValidTransition_Succeeds(WorkItemStatus from, WorkItemStatus to)
    {
        var item = WorkItemBuilder.New().WithStatus(from).Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UpdateAsync(Arg.Any<Domain.Entities.WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Domain.Entities.WorkItem>());

        var cmd = new ChangeWorkItemStatusCommand(item.Id, to);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(WorkItemStatus.ToDo, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.ToDo, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.InProgress)]
    public async Task Handle_InvalidTransition_ReturnsValidationError(WorkItemStatus from, WorkItemStatus to)
    {
        var item = WorkItemBuilder.New().WithStatus(from).Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var cmd = new ChangeWorkItemStatusCommand(item.Id, to);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid status transition");
    }

    [Fact]
    public async Task Handle_StatusChange_RecordsHistory()
    {
        var item = WorkItemBuilder.New().WithStatus(WorkItemStatus.ToDo).Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UpdateAsync(Arg.Any<Domain.Entities.WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Domain.Entities.WorkItem>());

        var cmd = new ChangeWorkItemStatusCommand(item.Id, WorkItemStatus.InProgress);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _historyService.Received(1).RecordAsync(
            Arg.Is<WorkItemHistoryEntry>(e => e.FieldName == "Status"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var itemId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns((Domain.Entities.WorkItem?)null);

        var cmd = new ChangeWorkItemStatusCommand(itemId, WorkItemStatus.InProgress);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
