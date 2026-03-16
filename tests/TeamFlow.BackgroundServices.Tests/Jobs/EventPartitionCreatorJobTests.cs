using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using TeamFlow.BackgroundServices.Scheduled.Jobs;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.BackgroundServices.Tests.Jobs;

[Collection("BackgroundServices")]
public sealed class EventPartitionCreatorJobTests(PostgresCollectionFixture fixture) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private TeamFlowDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;

    private readonly ILogger<EventPartitionCreatorJob> _logger = Substitute.For<ILogger<EventPartitionCreatorJob>>();
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

    [Fact]
    public async Task ExecuteInternal_CreatesPartitionForNextMonth()
    {
        var metric = new JobExecutionMetric { JobType = "EventPartitionCreatorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync();

        var sut = new EventPartitionCreatorJob(_logger, _dbContext);
        await sut.ExecuteJobAsync(_jobContext, metric);

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

        var sut = new EventPartitionCreatorJob(_logger, _dbContext);
        await sut.ExecuteJobAsync(_jobContext, metric1);

        var metric2 = new JobExecutionMetric { JobType = "EventPartitionCreatorJob", JobRunId = Guid.NewGuid(), StartedAt = DateTime.UtcNow };
        _dbContext.JobExecutionMetrics.Add(metric2);
        await _dbContext.SaveChangesAsync();

        var act = () => sut.ExecuteJobAsync(_jobContext, metric2);

        await act.Should().NotThrowAsync();
        metric2.RecordsProcessed.Should().Be(1);
    }
}
