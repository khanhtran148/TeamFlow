using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Retros.CreateRetroActionItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class CreateRetroActionItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesActionItem()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var cmd = new CreateRetroActionItemCommand(session.Id, null, "Fix CI pipeline", "Description", null, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Fix CI pipeline");
    }

    [Fact]
    public async Task Handle_WithBacklogLink_CreatesWorkItem()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var cmd = new CreateRetroActionItemCommand(session.Id, null, "Fix CI", null, null, null, true);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedTaskId.Should().NotBeNull();

        var linkedWorkItem = await DbContext.WorkItems
            .SingleOrDefaultAsync(w => w.Id == result.Value.LinkedTaskId);
        linkedWorkItem.Should().NotBeNull();
        linkedWorkItem!.Type.Should().Be(WorkItemType.Task);
        linkedWorkItem.Title.Should().Be("Fix CI");
    }

    [Fact]
    public async Task Handle_WithoutBacklogLink_DoesNotCreateWorkItem()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var workItemCountBefore = await DbContext.WorkItems.CountAsync();

        var cmd = new CreateRetroActionItemCommand(session.Id, null, "Fix CI", null, null, null, false);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedTaskId.Should().BeNull();
        var workItemCountAfter = await DbContext.WorkItems.CountAsync();
        workItemCountAfter.Should().Be(workItemCountBefore);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyTitle_Fails(string? title)
    {
        var validator = new CreateRetroActionItemValidator();
        var cmd = new CreateRetroActionItemCommand(Guid.NewGuid(), null, title!, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
