using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.StartSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class StartSprintTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_PlanningSprintWithItemsAndDates_StartSucceeds()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b.WithSprint(sprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SprintStatus.Active);
    }

    [Fact]
    public async Task Handle_SprintWithNoItems_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one item");
    }

    [Fact]
    public async Task Handle_AnotherActiveSprint_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var activeSprint = SprintBuilder.New().WithProject(project.Id).Active().Build();
        DbContext.Sprints.Add(activeSprint);

        var newSprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithDates(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 14))
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(newSprint);
        await SeedWorkItemAsync(project.Id, b => b.WithSprint(newSprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new StartSprintCommand(newSprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already active");
    }

    [Fact]
    public async Task Handle_SprintWithoutDates_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b.WithSprint(sprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("start and end dates");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        var cmd = new StartSprintCommand(Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}

[Collection("Sprints")]
public sealed class StartSprintDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NonTeamManager_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}

[Collection("Sprints")]
public sealed class StartSprintPublishTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly CapturingPublisher _capturing = new();

    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPublisher>(_ => _capturing);

    [Fact]
    public async Task Handle_StartSprint_PublishesSprintStartedDomainEvent()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b.WithSprint(sprint.Id));
        await DbContext.SaveChangesAsync();

        var cmd = new StartSprintCommand(sprint.Id);
        await Sender.Send(cmd);

        _capturing.HasPublished<SprintStartedDomainEvent>().Should().BeTrue();
        var evt = _capturing.GetPublished<SprintStartedDomainEvent>();
        evt.SprintId.Should().Be(sprint.Id);
    }
}
