using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.DeleteWorkItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class DeleteWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public DeleteWorkItemTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private DeleteWorkItemHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ExistingItem_SoftDeletes()
    {
        var item = WorkItemBuilder.New().Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.SoftDeleteCascadeAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns([item.Id]);

        var result = await CreateHandler().Handle(new DeleteWorkItemCommand(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _workItemRepo.Received(1).SoftDeleteCascadeAsync(item.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var itemId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns((WorkItem?)null);

        var result = await CreateHandler().Handle(new DeleteWorkItemCommand(itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DeleteEpic_RecordsHistoryForEachDeleted()
    {
        var epic = WorkItemBuilder.New().AsEpic().Build();
        var storyId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(epic.Id, Arg.Any<CancellationToken>()).Returns(epic);
        _workItemRepo.SoftDeleteCascadeAsync(epic.Id, Arg.Any<CancellationToken>())
            .Returns([epic.Id, storyId]);

        await CreateHandler().Handle(new DeleteWorkItemCommand(epic.Id), CancellationToken.None);

        await _historyService.Received(2).RecordAsync(
            Arg.Any<WorkItemHistoryEntry>(),
            Arg.Any<CancellationToken>());
    }
}
