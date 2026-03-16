using FluentAssertions;
using MediatR;
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
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.BackgroundServices.Tests.Jobs;

[Collection("BackgroundServices")]
public sealed class StaleItemDetectorJobTests(PostgresCollectionFixture fixture) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TeamFlowDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;

    private readonly IBroadcastService _broadcastService = Substitute.For<IBroadcastService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<StaleItemDetectorJob> _logger = Substitute.For<ILogger<StaleItemDetectorJob>>();
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

        _jobContext.CancellationToken.Returns(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        _scope.Dispose();
        await _provider.DisposeAsync();
    }

    private async Task SetWorkItemUpdatedAt(Guid itemId, DateTime updatedAt)
    {
        await _dbContext.Database.ExecuteSqlAsync(
            $"UPDATE work_items SET updated_at = {updatedAt} WHERE id = {itemId}");
        _dbContext.ChangeTracker.Clear();
    }

    private async Task<Project> AddProjectAsync(string status = "Active")
    {
        var org = OrganizationBuilder.New().Build();
        _dbContext.Organizations.Add(org);
        await _dbContext.SaveChangesAsync();

        var project = ProjectBuilder.New()
            .WithOrganization(org.Id)
            .WithStatus(status)
            .Build();
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        return project;
    }

    [Fact]
    public async Task ExecuteInternal_ItemNotUpdatedIn14Days_FlagsAsStale()
    {
        var project = await AddProjectAsync();

        var item = WorkItemBuilder.New()
            .WithProject(project.Id)
            .WithStatus(WorkItemStatus.InProgress)
            .Build();

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();

        await SetWorkItemUpdatedAt(item.Id, DateTime.UtcNow.AddDays(-15));

        var metric = new JobExecutionMetric { JobType = "StaleItemDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new StaleItemDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().BeGreaterThanOrEqualTo(1);
        await _publisher.Received(1).Publish(
            Arg.Is<WorkItemStaleFlaggedDomainEvent>(e =>
                e.WorkItemId == item.Id &&
                e.DaysSinceUpdate >= 14),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(WorkItemStatus.Done)]
    [InlineData(WorkItemStatus.Rejected)]
    public async Task ExecuteInternal_DoneAndRejectedItems_AreSkipped(WorkItemStatus status)
    {
        var project = await AddProjectAsync();

        var item = WorkItemBuilder.New()
            .WithProject(project.Id)
            .WithStatus(status)
            .Build();

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();

        await SetWorkItemUpdatedAt(item.Id, DateTime.UtcNow.AddDays(-30));

        var metric = new JobExecutionMetric { JobType = "StaleItemDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new StaleItemDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInternal_ItemInArchivedProject_IsSkipped()
    {
        var project = await AddProjectAsync("Archived");

        var item = WorkItemBuilder.New()
            .WithProject(project.Id)
            .WithStatus(WorkItemStatus.ToDo)
            .Build();

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();

        await SetWorkItemUpdatedAt(item.Id, DateTime.UtcNow.AddDays(-20));

        var metric = new JobExecutionMetric { JobType = "StaleItemDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new StaleItemDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInternal_ItemInActiveSprint_SeverityIsCritical()
    {
        var project = await AddProjectAsync();

        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .Active()
            .WithDates(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .Build();

        _dbContext.Sprints.Add(sprint);
        await _dbContext.SaveChangesAsync();

        var item = WorkItemBuilder.New()
            .WithProject(project.Id)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.InProgress)
            .Build();

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();

        await SetWorkItemUpdatedAt(item.Id, DateTime.UtcNow.AddDays(-15));

        var metric = new JobExecutionMetric { JobType = "StaleItemDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new StaleItemDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        await _publisher.Received(1).Publish(
            Arg.Is<WorkItemStaleFlaggedDomainEvent>(e =>
                e.Severity == "Critical"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInternal_ItemInRelease_SeverityIsHigh()
    {
        var project = await AddProjectAsync();

        var release = ReleaseBuilder.New()
            .WithProject(project.Id)
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var item = WorkItemBuilder.New()
            .WithProject(project.Id)
            .WithRelease(release.Id)
            .WithStatus(WorkItemStatus.ToDo)
            .Build();

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();

        await SetWorkItemUpdatedAt(item.Id, DateTime.UtcNow.AddDays(-15));

        var metric = new JobExecutionMetric { JobType = "StaleItemDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new StaleItemDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        await _publisher.Received(1).Publish(
            Arg.Is<WorkItemStaleFlaggedDomainEvent>(e =>
                e.Severity == "High"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInternal_SoftDeletedItem_IsSkipped()
    {
        var project = await AddProjectAsync();

        var item = WorkItemBuilder.New()
            .WithProject(project.Id)
            .WithStatus(WorkItemStatus.InProgress)
            .Build();
        item.DeletedAt = DateTime.UtcNow.AddDays(-5);

        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "StaleItemDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new StaleItemDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
    }
}
