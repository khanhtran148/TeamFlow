using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.RemoveItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class RemoveItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_RemoveItemFromSprint_SetsSprintIdNull()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id, b => b.WithSprint(sprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveItemFromSprintCommand(sprint.Id, workItem.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.FindAsync(workItem.Id);
        updated!.SprintId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ItemNotInSprint_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id); // no sprint
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveItemFromSprintCommand(sprint.Id, workItem.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("does not belong to this sprint");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        var cmd = new RemoveItemFromSprintCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}

[Collection("Sprints")]
public sealed class RemoveItemPublishTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly CapturingPublisher _capturing = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPublisher>(_ => _capturing);

    [Fact]
    public async Task Handle_RemoveItem_PublishesSprintItemRemovedDomainEvent()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id, b => b.WithSprint(sprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new RemoveItemFromSprintCommand(sprint.Id, workItem.Id);
        await Sender.Send(cmd);

        _capturing.HasPublished<SprintItemRemovedDomainEvent>().Should().BeTrue();
        var evt = _capturing.GetPublished<SprintItemRemovedDomainEvent>();
        evt.SprintId.Should().Be(sprint.Id);
        evt.WorkItemId.Should().Be(workItem.Id);
    }
}
