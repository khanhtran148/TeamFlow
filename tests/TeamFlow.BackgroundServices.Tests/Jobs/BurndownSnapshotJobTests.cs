using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.Scheduled.Jobs;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.BackgroundServices.Tests.Jobs;

[Collection("BackgroundServices")]
public sealed class BurndownSnapshotJobTests(PostgresCollectionFixture fixture) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TeamFlowDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;
    private Project _project = null!;

    private readonly IBroadcastService _broadcastService = Substitute.For<IBroadcastService>();
    private readonly ILogger<BurndownSnapshotJob> _logger = Substitute.For<ILogger<BurndownSnapshotJob>>();
    private readonly IJobExecutionContext _jobContext = Substitute.For<IJobExecutionContext>();

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

        _project = ProjectBuilder.New().WithOrganization(PostgresCollectionFixture.SeedOrgId).Build();
        _dbContext.Projects.Add(_project);
        await _dbContext.SaveChangesAsync();

        _jobContext.CancellationToken.Returns(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        _scope.Dispose();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteInternal_ActiveSprint_CreatesBurndownDataPoint()
    {
        var sprint = SprintBuilder.New()
            .WithProject(_project.Id)
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .Build();

        var workItem1 = WorkItemBuilder.New()
            .WithProject(_project.Id)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .WithEstimation(3)
            .Build();

        var workItem2 = WorkItemBuilder.New()
            .WithProject(_project.Id)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.InProgress)
            .WithEstimation(5)
            .Build();

        _dbContext.Sprints.Add(sprint);
        _dbContext.WorkItems.AddRange(workItem1, workItem2);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "BurndownSnapshotJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new BurndownSnapshotJob(_logger, _dbContext, _broadcastService);
        await sut.ExecuteJobAsync(_jobContext, metric);

        var dataPoint = await _dbContext.BurndownDataPoints
            .FirstOrDefaultAsync(b => b.SprintId == sprint.Id);

        dataPoint.Should().NotBeNull();
        dataPoint!.CompletedPoints.Should().Be(3);
        dataPoint.RemainingPoints.Should().Be(5);
        metric.RecordsProcessed.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInternal_AlreadyHasTodayDataPoint_SkipsSprint()
    {
        var sprint = SprintBuilder.New()
            .WithProject(_project.Id)
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .Build();

        var existingPoint = BurndownDataPointBuilder.New()
            .WithSprint(sprint.Id)
            .WithDate(DateOnly.FromDateTime(DateTime.UtcNow))
            .Build();

        _dbContext.Sprints.Add(sprint);
        _dbContext.BurndownDataPoints.Add(existingPoint);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "BurndownSnapshotJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new BurndownSnapshotJob(_logger, _dbContext, _broadcastService);
        await sut.ExecuteJobAsync(_jobContext, metric);

        var count = await _dbContext.BurndownDataPoints.CountAsync(b => b.SprintId == sprint.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInternal_ZeroActiveSprints_HandlesGracefully()
    {
        var metric = new JobExecutionMetric { JobType = "BurndownSnapshotJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new BurndownSnapshotJob(_logger, _dbContext, _broadcastService);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
        metric.RecordsFailed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInternal_RemainingExceedsIdealBy20Percent_LogsAtRiskWarning()
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var sprint = SprintBuilder.New()
            .WithProject(_project.Id)
            .Active()
            .WithDates(startDate, endDate)
            .Build();

        var doneItem = WorkItemBuilder.New()
            .WithProject(_project.Id)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .WithEstimation(1)
            .Build();

        var todoItem = WorkItemBuilder.New()
            .WithProject(_project.Id)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.ToDo)
            .WithEstimation(9)
            .Build();

        _dbContext.Sprints.Add(sprint);
        _dbContext.WorkItems.AddRange(doneItem, todoItem);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "BurndownSnapshotJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new BurndownSnapshotJob(_logger, _dbContext, _broadcastService);
        await sut.ExecuteJobAsync(_jobContext, metric);

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("At Risk")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteInternal_ActiveSprint_BroadcastsSignalR()
    {
        var sprint = SprintBuilder.New()
            .WithProject(_project.Id)
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(11)))
            .Build();

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "BurndownSnapshotJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new BurndownSnapshotJob(_logger, _dbContext, _broadcastService);
        await sut.ExecuteJobAsync(_jobContext, metric);

        await _broadcastService.Received(1).BroadcastToSprintAsync(
            sprint.Id,
            "burndown.updated",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
