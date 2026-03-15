using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.UpdateWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class UpdateWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public UpdateWorkItemTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UpdateWorkItemHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var item = WorkItemBuilder.New().WithTitle("Old Title").Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UpdateAsync(Arg.Any<Domain.Entities.WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Domain.Entities.WorkItem>());

        var cmd = new UpdateWorkItemCommand(item.Id, "New Title", "New Desc", Priority.High, 5m, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("New Title");
        result.Value.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var itemId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns((Domain.Entities.WorkItem?)null);

        var cmd = new UpdateWorkItemCommand(itemId, "Title", null, null, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TitleChange_RecordsHistory()
    {
        var item = WorkItemBuilder.New().WithTitle("Old Title").Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UpdateAsync(Arg.Any<Domain.Entities.WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Domain.Entities.WorkItem>());

        var cmd = new UpdateWorkItemCommand(item.Id, "New Title", null, null, null, null);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _historyService.Received().RecordAsync(
            Arg.Is<WorkItemHistoryEntry>(e => e.FieldName == "Title"),
            Arg.Any<CancellationToken>());
    }
}
