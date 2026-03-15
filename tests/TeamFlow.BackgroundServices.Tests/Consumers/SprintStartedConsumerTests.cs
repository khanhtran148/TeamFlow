using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MassTransit;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.Consumers;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.BackgroundServices.Tests.Consumers;

public sealed class SprintStartedConsumerTests : IDisposable
{
    private readonly TeamFlowDbContext _dbContext;
    private readonly IBroadcastService _broadcastService;
    private readonly ILogger<SprintStartedConsumer> _logger;
    private readonly SprintStartedConsumer _sut;

    public SprintStartedConsumerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _broadcastService = Substitute.For<IBroadcastService>();
        _logger = Substitute.For<ILogger<SprintStartedConsumer>>();
        _sut = new SprintStartedConsumer(_logger, _dbContext, _broadcastService);
    }

    [Fact]
    public async Task ConsumeInternal_CreatesOnStartSnapshot()
    {
        var sprint = SprintBuilder.New()
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
            .Build();

        var workItem = WorkItemBuilder.New()
            .WithProject(sprint.ProjectId)
            .WithSprint(sprint.Id)
            .WithEstimation(5)
            .Build();

        _dbContext.Sprints.Add(sprint);
        _dbContext.WorkItems.Add(workItem);
        await _dbContext.SaveChangesAsync();

        var @event = new SprintStartedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate!.Value,
            sprint.EndDate!.Value,
            Guid.NewGuid());

        var consumeContext = Substitute.For<ConsumeContext<SprintStartedDomainEvent>>();
        consumeContext.Message.Returns(@event);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(consumeContext);

        var snapshot = await _dbContext.SprintSnapshots
            .FirstOrDefaultAsync(s => s.SprintId == sprint.Id);

        snapshot.Should().NotBeNull();
        snapshot!.SnapshotType.Should().Be("OnStart");
        snapshot.IsFinal.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeInternal_InitializesBurndownDataPoint()
    {
        var sprint = SprintBuilder.New()
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
            .Build();

        var workItem = WorkItemBuilder.New()
            .WithProject(sprint.ProjectId)
            .WithSprint(sprint.Id)
            .WithEstimation(8)
            .Build();

        _dbContext.Sprints.Add(sprint);
        _dbContext.WorkItems.Add(workItem);
        await _dbContext.SaveChangesAsync();

        var @event = new SprintStartedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate!.Value,
            sprint.EndDate!.Value,
            Guid.NewGuid());

        var consumeContext = Substitute.For<ConsumeContext<SprintStartedDomainEvent>>();
        consumeContext.Message.Returns(@event);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(consumeContext);

        var dataPoint = await _dbContext.BurndownDataPoints
            .FirstOrDefaultAsync(b => b.SprintId == sprint.Id);

        dataPoint.Should().NotBeNull();
        dataPoint!.RemainingPoints.Should().Be(8);
        dataPoint.CompletedPoints.Should().Be(0);
    }

    [Fact]
    public async Task ConsumeInternal_BroadcastsSprintStarted()
    {
        var sprint = SprintBuilder.New()
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
            .Build();

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        var @event = new SprintStartedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate!.Value,
            sprint.EndDate!.Value,
            Guid.NewGuid());

        var consumeContext = Substitute.For<ConsumeContext<SprintStartedDomainEvent>>();
        consumeContext.Message.Returns(@event);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(consumeContext);

        await _broadcastService.Received(1).BroadcastToProjectAsync(
            sprint.ProjectId,
            "sprint.started",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
