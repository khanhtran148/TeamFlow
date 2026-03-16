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
public sealed class ReleaseOverdueDetectorJobTests(PostgresCollectionFixture fixture) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TeamFlowDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;
    private Project _project = null!;

    private readonly IBroadcastService _broadcastService = Substitute.For<IBroadcastService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<ReleaseOverdueDetectorJob> _logger = Substitute.For<ILogger<ReleaseOverdueDetectorJob>>();
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
    public async Task ExecuteInternal_ReleasePastDueDate_SetsStatusOverdue()
    {
        var release = ReleaseBuilder.New()
            .WithProject(_project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new ReleaseOverdueDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        var updated = await _dbContext.Releases.FirstAsync(r => r.Id == release.Id);
        updated.Status.Should().Be(ReleaseStatus.Overdue);
        metric.RecordsProcessed.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInternal_AlreadyOverdueRelease_DoesNotDoubleTransition()
    {
        var release = ReleaseBuilder.New()
            .WithProject(_project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)))
            .WithStatus(ReleaseStatus.Overdue)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new ReleaseOverdueDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
        await _publisher.DidNotReceive().Publish(
            Arg.Any<ReleaseOverdueDetectedDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInternal_ReleasedRelease_IsIgnored()
    {
        var release = ReleaseBuilder.New()
            .WithProject(_project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .Released()
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new ReleaseOverdueDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInternal_OverdueRelease_PublishesDomainEvent()
    {
        var release = ReleaseBuilder.New()
            .WithProject(_project.Id)
            .WithName("v2.0")
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new ReleaseOverdueDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        await _publisher.Received(1).Publish(
            Arg.Is<ReleaseOverdueDetectedDomainEvent>(e =>
                e.ReleaseId == release.Id &&
                e.ReleaseName == "v2.0"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInternal_OverdueRelease_BroadcastsSignalR()
    {
        var release = ReleaseBuilder.New()
            .WithProject(_project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new ReleaseOverdueDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        await sut.ExecuteJobAsync(_jobContext, metric);

        await _broadcastService.Received(1).BroadcastToProjectAsync(
            release.ProjectId,
            "release.overdue_detected",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
