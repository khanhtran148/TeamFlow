using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Sprints.DeleteSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class DeleteSprintTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_PlanningSprintDelete_Succeeds()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var sprintId = sprint.Id;
        var cmd = new DeleteSprintCommand(sprintId);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Sprints.FindAsync(sprintId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeletePlanningSprintWithItems_UnlinksItems()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        var item = await SeedWorkItemAsync(project.Id, b => b.WithSprint(sprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new DeleteSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var unlinked = await DbContext.WorkItems.FindAsync(item.Id);
        unlinked!.SprintId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActiveSprint_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Active().Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new DeleteSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not in Planning status");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        var cmd = new DeleteSprintCommand(Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}
