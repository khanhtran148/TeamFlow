using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.WorkItems.UpdateWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class UpdateWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithTitle("Old Title"));

        var cmd = new UpdateWorkItemCommand(item.Id, "New Title", "New Desc", Priority.High, 5m, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("New Title");
        result.Value.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var cmd = new UpdateWorkItemCommand(Guid.NewGuid(), "Title", null, null, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TitleChange_RecordsHistory()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.WithTitle("Old Title"));

        var cmd = new UpdateWorkItemCommand(item.Id, "New Title", null, null, null, null);
        await Sender.Send(cmd);

        DbContext.ChangeTracker.Clear();
        var historyEntry = await DbContext.Set<Domain.Entities.WorkItemHistory>()
            .Where(h => h.WorkItemId == item.Id && h.FieldName == "Title")
            .FirstOrDefaultAsync();
        historyEntry.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyTitle_Fails(string? title)
    {
        var validator = new UpdateWorkItemValidator();
        var cmd = new UpdateWorkItemCommand(Guid.NewGuid(), title!, null, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptyWorkItemId_Fails()
    {
        var validator = new UpdateWorkItemValidator();
        var cmd = new UpdateWorkItemCommand(Guid.Empty, "Title", null, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_TitleTooLong_Fails()
    {
        var validator = new UpdateWorkItemValidator();
        var cmd = new UpdateWorkItemCommand(Guid.NewGuid(), new string('A', 501), null, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NegativeEstimation_Fails()
    {
        var validator = new UpdateWorkItemValidator();
        var cmd = new UpdateWorkItemCommand(Guid.NewGuid(), "Title", null, null, -1m, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var validator = new UpdateWorkItemValidator();
        var cmd = new UpdateWorkItemCommand(Guid.NewGuid(), "Title", "Desc", Priority.High, 5m, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
