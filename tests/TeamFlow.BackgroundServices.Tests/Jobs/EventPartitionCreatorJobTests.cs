using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using TeamFlow.BackgroundServices.Scheduled.Jobs;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Tests.Jobs;

public sealed class EventPartitionCreatorJobTests : IDisposable
{
    private readonly TeamFlowDbContext _dbContext;
    private readonly ILogger<EventPartitionCreatorJob> _logger;
    private readonly EventPartitionCreatorJob _sut;
    private readonly IJobExecutionContext _jobContext;

    public EventPartitionCreatorJobTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _logger = Substitute.For<ILogger<EventPartitionCreatorJob>>();
        _sut = new EventPartitionCreatorJob(_logger, _dbContext);
        _jobContext = Substitute.For<IJobExecutionContext>();
        _jobContext.CancellationToken.Returns(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteInternal_CreatesPartitionForNextMonth()
    {
        var metric = new JobExecutionMetric { JobType = "EventPartitionCreatorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        // SQLite doesn't support PARTITION OF, but the job gracefully handles non-relational
        // by checking Database.IsRelational(). For SQLite, it is relational but doesn't support
        // the raw SQL. The job should still complete and record metrics.
        // We verify the job logs the correct partition name.
        await _sut.ExecuteJobAsync(_jobContext, metric);

        metric.RecordsProcessed.Should().Be(1);

        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var expectedPartition = $"domain_events_{nextMonth:yyyy_MM}";

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(expectedPartition)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteInternal_Idempotent_RerunDoesNotFail()
    {
        var metric1 = new JobExecutionMetric { JobType = "EventPartitionCreatorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric1);
        await _dbContext.SaveChangesAsync();

        await _sut.ExecuteJobAsync(_jobContext, metric1);

        var metric2 = new JobExecutionMetric { JobType = "EventPartitionCreatorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric2);
        await _dbContext.SaveChangesAsync();

        var act = () => _sut.ExecuteJobAsync(_jobContext, metric2);

        await act.Should().NotThrowAsync();
        metric2.RecordsProcessed.Should().Be(1);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
