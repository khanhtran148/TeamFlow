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

public sealed class SprintCompletedConsumerTests : IDisposable
{
    private readonly TeamFlowDbContext _dbContext;
    private readonly IBroadcastService _broadcastService;
    private readonly ILogger<SprintCompletedConsumer> _logger;
    private readonly SprintCompletedConsumer _sut;

    public SprintCompletedConsumerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _broadcastService = Substitute.For<IBroadcastService>();
        _logger = Substitute.For<ILogger<SprintCompletedConsumer>>();
        _sut = new SprintCompletedConsumer(_logger, _dbContext, _broadcastService);
    }

    [Fact]
    public async Task ConsumeInternal_CreatesFinalSnapshot()
    {
        var sprint = SprintBuilder.New()
            .Completed()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), DateOnly.FromDateTime(DateTime.UtcNow))
            .Build();

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        var @event = new SprintCompletedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            PlannedPoints: 20,
            CompletedPoints: 15,
            Guid.NewGuid());

        var consumeContext = Substitute.For<ConsumeContext<SprintCompletedDomainEvent>>();
        consumeContext.Message.Returns(@event);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(consumeContext);

        var snapshot = await _dbContext.SprintSnapshots
            .FirstOrDefaultAsync(s => s.SprintId == sprint.Id);

        snapshot.Should().NotBeNull();
        snapshot!.SnapshotType.Should().Be("OnClose");
        snapshot.IsFinal.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeInternal_RecordsVelocity()
    {
        var sprint = SprintBuilder.New()
            .Completed()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), DateOnly.FromDateTime(DateTime.UtcNow))
            .Build();

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        var @event = new SprintCompletedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            PlannedPoints: 20,
            CompletedPoints: 15,
            Guid.NewGuid());

        var consumeContext = Substitute.For<ConsumeContext<SprintCompletedDomainEvent>>();
        consumeContext.Message.Returns(@event);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(consumeContext);

        var velocity = await _dbContext.TeamVelocityHistories
            .FirstOrDefaultAsync(v => v.SprintId == sprint.Id);

        velocity.Should().NotBeNull();
        velocity!.PlannedPoints.Should().Be(20);
        velocity.CompletedPoints.Should().Be(15);
        velocity.Velocity.Should().Be(15);
        velocity.ProjectId.Should().Be(sprint.ProjectId);
    }

    [Fact]
    public async Task ConsumeInternal_BroadcastsSprintCompleted()
    {
        var sprint = SprintBuilder.New()
            .Completed()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), DateOnly.FromDateTime(DateTime.UtcNow))
            .Build();

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        var @event = new SprintCompletedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            PlannedPoints: 20,
            CompletedPoints: 15,
            Guid.NewGuid());

        var consumeContext = Substitute.For<ConsumeContext<SprintCompletedDomainEvent>>();
        consumeContext.Message.Returns(@event);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await _sut.Consume(consumeContext);

        await _broadcastService.Received(1).BroadcastToProjectAsync(
            sprint.ProjectId,
            "sprint.completed",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
