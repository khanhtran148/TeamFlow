using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Backlog.MarkReadyForSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

public sealed class MarkReadyTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public MarkReadyTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private MarkReadyForSprintHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidItem_MarksReady()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var result = await CreateHandler().Handle(
            new MarkReadyForSprintCommand(workItem.Id, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workItem.IsReadyForSprint.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidItem_UnmarksReady()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        workItem.IsReadyForSprint = true;
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var result = await CreateHandler().Handle(
            new MarkReadyForSprintCommand(workItem.Id, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workItem.IsReadyForSprint.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        _workItemRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        var result = await CreateHandler().Handle(
            new MarkReadyForSprintCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.WorkItem_Edit, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new MarkReadyForSprintCommand(workItem.Id, true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_RecordsHistory()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        await CreateHandler().Handle(
            new MarkReadyForSprintCommand(workItem.Id, true), CancellationToken.None);

        await _historyService.Received(1).RecordAsync(
            Arg.Is<WorkItemHistoryEntry>(e => e.FieldName == "IsReadyForSprint"),
            Arg.Any<CancellationToken>());
    }
}
