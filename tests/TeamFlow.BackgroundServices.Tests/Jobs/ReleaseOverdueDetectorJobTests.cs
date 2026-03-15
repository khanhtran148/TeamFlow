using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.Scheduled.Jobs;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.BackgroundServices.Tests.Jobs;

public sealed class ReleaseOverdueDetectorJobTests : IDisposable
{
    private readonly TeamFlowDbContext _dbContext;
    private readonly IBroadcastService _broadcastService;
    private readonly IPublisher _publisher;
    private readonly ILogger<ReleaseOverdueDetectorJob> _logger;
    private readonly ReleaseOverdueDetectorJob _sut;
    private readonly IJobExecutionContext _jobContext;

    public ReleaseOverdueDetectorJobTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _broadcastService = Substitute.For<IBroadcastService>();
        _publisher = Substitute.For<IPublisher>();
        _logger = Substitute.For<ILogger<ReleaseOverdueDetectorJob>>();
        _sut = new ReleaseOverdueDetectorJob(_logger, _dbContext, _broadcastService, _publisher);
        _jobContext = Substitute.For<IJobExecutionContext>();
        _jobContext.CancellationToken.Returns(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteInternal_ReleasePastDueDate_SetsStatusOverdue()
    {
        var release = ReleaseBuilder.New()
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        await _sut.ExecuteJobAsync(_jobContext, metric);

        var updated = await _dbContext.Releases.FirstAsync(r => r.Id == release.Id);
        updated.Status.Should().Be(ReleaseStatus.Overdue);
        metric.RecordsProcessed.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInternal_AlreadyOverdueRelease_DoesNotDoubleTransition()
    {
        var release = ReleaseBuilder.New()
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)))
            .WithStatus(ReleaseStatus.Overdue)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        await _sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
        await _publisher.DidNotReceive().Publish(
            Arg.Any<ReleaseOverdueDetectedDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteInternal_ReleasedRelease_IsIgnored()
    {
        var release = ReleaseBuilder.New()
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .Released()
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        await _sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInternal_OverdueRelease_PublishesDomainEvent()
    {
        var release = ReleaseBuilder.New()
            .WithName("v2.0")
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        await _sut.ExecuteJobAsync(_jobContext, metric);

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
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();

        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync();

        var metric = new JobExecutionMetric { JobType = "ReleaseOverdueDetectorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        await _sut.ExecuteJobAsync(_jobContext, metric);

        await _broadcastService.Received(1).BroadcastToProjectAsync(
            release.ProjectId,
            "release.overdue_detected",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
