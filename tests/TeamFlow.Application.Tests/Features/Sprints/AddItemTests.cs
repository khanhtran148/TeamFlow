using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.AddItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class AddItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_AddItemToPlanningSprintSucceeds()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id);
        await DbContext.SaveChangesAsync();

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.FindAsync(workItem.Id);
        updated!.SprintId.Should().Be(sprint.Id);
    }

    [Fact]
    public async Task Handle_ItemAlreadyInAnotherSprint_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        var otherSprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.AddRange(sprint, otherSprint);
        var workItem = await SeedWorkItemAsync(project.Id, b => b.WithSprint(otherSprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already belongs to another sprint");
    }

    [Fact]
    public async Task Handle_AddItemToCompletedSprint_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Completed().Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new AddItemToSprintCommand(sprint.Id, Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot add items");
    }

    [Fact]
    public async Task Handle_AddItem_RecordsHistory()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id);
        await DbContext.SaveChangesAsync();

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        await Sender.Send(cmd);

        DbContext.ChangeTracker.Clear();
        var historyEntries = await DbContext.WorkItemHistories
            .Where(h => h.WorkItemId == workItem.Id && h.ActionType == "SprintAssigned")
            .ToListAsync();
        historyEntries.Should().HaveCountGreaterThan(0);
    }
}

[Collection("Sprints")]
public sealed class AddItemDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_AddItemToActiveSprint_WhenPermissionDenied_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Active().Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id);
        await DbContext.SaveChangesAsync();

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}

[Collection("Sprints")]
public sealed class AddItemPublishTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly CapturingPublisher _capturing = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPublisher>(_ => _capturing);

    [Fact]
    public async Task Handle_AddItem_PublishesSprintItemAddedDomainEvent()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        var workItem = await SeedWorkItemAsync(project.Id);
        await DbContext.SaveChangesAsync();

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        await Sender.Send(cmd);

        _capturing.HasPublished<SprintItemAddedDomainEvent>().Should().BeTrue();
        var evt = _capturing.GetPublished<SprintItemAddedDomainEvent>();
        evt.SprintId.Should().Be(sprint.Id);
        evt.WorkItemId.Should().Be(workItem.Id);
    }
}
