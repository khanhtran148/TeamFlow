using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MassTransit;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.Consumers;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.BackgroundServices.Tests.Consumers;

[Collection("BackgroundServices")]
public sealed class SprintCompletedConsumerTests(PostgresCollectionFixture fixture) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TeamFlowDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;

    private readonly IBroadcastService _broadcastService = Substitute.For<IBroadcastService>();
    private readonly ILogger<SprintCompletedConsumer> _logger = Substitute.For<ILogger<SprintCompletedConsumer>>();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<TeamFlowDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString, npgsql =>
                npgsql.MigrationsAssembly("TeamFlow.Infrastructure")));
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        _scope.Dispose();
        await _provider.DisposeAsync();
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

        var sut = new SprintCompletedConsumer(_logger, _dbContext, _broadcastService);
        await sut.Consume(consumeContext);

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

        var sut = new SprintCompletedConsumer(_logger, _dbContext, _broadcastService);
        await sut.Consume(consumeContext);

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

        var sut = new SprintCompletedConsumer(_logger, _dbContext, _broadcastService);
        await sut.Consume(consumeContext);

        await _broadcastService.Received(1).BroadcastToProjectAsync(
            sprint.ProjectId,
            "sprint.completed",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
