using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.WorkItems.ChangeStatus;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class ChangeStatusTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Theory]
    [InlineData(WorkItemStatus.ToDo, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.ToDo)]
    public async Task Handle_ValidTransition_Succeeds(WorkItemStatus from, WorkItemStatus to)
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithStatus(from));

        var result = await Sender.Send(new ChangeWorkItemStatusCommand(item.Id, to));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(WorkItemStatus.ToDo, WorkItemStatus.InReview)]
    [InlineData(WorkItemStatus.ToDo, WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.InReview, WorkItemStatus.InProgress)]
    public async Task Handle_InvalidTransition_ReturnsValidationError(WorkItemStatus from, WorkItemStatus to)
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithStatus(from));

        var result = await Sender.Send(new ChangeWorkItemStatusCommand(item.Id, to));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid status transition");
    }

    [Fact]
    public async Task Handle_StatusChange_RecordsHistory()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithStatus(WorkItemStatus.ToDo));

        await Sender.Send(new ChangeWorkItemStatusCommand(item.Id, WorkItemStatus.InProgress));

        DbContext.ChangeTracker.Clear();
        var historyEntry = await DbContext.Set<Domain.Entities.WorkItemHistory>()
            .Where(h => h.WorkItemId == item.Id && h.FieldName == "Status")
            .FirstOrDefaultAsync();
        historyEntry.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new ChangeWorkItemStatusCommand(Guid.NewGuid(), WorkItemStatus.InProgress));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
