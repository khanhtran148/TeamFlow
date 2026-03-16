using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.CompleteSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class CompleteSprintTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ActiveSprint_CompletesSuccessfully()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithDates(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15))
            .Active()
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .WithEstimation(5));
        await DbContext.SaveChangesAsync();

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SprintStatus.Completed);
    }

    [Fact]
    public async Task Handle_PlanningSprintComplete_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only an Active sprint");
    }

    [Fact]
    public async Task Handle_CompleteSprint_CarriesOverIncompleteItems()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .Active()
            .Build();
        DbContext.Sprints.Add(sprint);
        var incompleteItem = await SeedWorkItemAsync(project.Id, b => b
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.InProgress));
        await SeedWorkItemAsync(project.Id, b => b
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done));
        await DbContext.SaveChangesAsync();

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var unlinked = await DbContext.WorkItems.FindAsync(incompleteItem.Id);
        unlinked!.SprintId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        var cmd = new CompleteSprintCommand(Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}

[Collection("Sprints")]
public sealed class CompleteSprintDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Active().Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}

[Collection("Sprints")]
public sealed class CompleteSprintPublishTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly CapturingPublisher _capturing = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPublisher>(_ => _capturing);

    [Fact]
    public async Task Handle_CompleteSprint_PublishesSprintCompletedDomainEvent()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .Active()
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .WithEstimation(8));
        await DbContext.SaveChangesAsync();

        var cmd = new CompleteSprintCommand(sprint.Id);
        await Sender.Send(cmd);

        _capturing.HasPublished<SprintCompletedDomainEvent>().Should().BeTrue();
        var evt = _capturing.GetPublished<SprintCompletedDomainEvent>();
        evt.SprintId.Should().Be(sprint.Id);
        evt.PlannedPoints.Should().Be(8);
        evt.CompletedPoints.Should().Be(8);
    }
}
