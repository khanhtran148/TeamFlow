using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.GetReleaseDetail;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class GetReleaseDetailTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_MixedStatuses_ReturnsCorrectProgressCounts()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.Done).WithEstimation(5));
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.InProgress).WithEstimation(3));
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.ToDo).WithEstimation(8));
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.InReview).WithEstimation(2));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseDetailQuery(release.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Progress.TotalItems.Should().Be(4);
        result.Value.Progress.DoneItems.Should().Be(1);
        result.Value.Progress.InProgressItems.Should().Be(2);
        result.Value.Progress.ToDoItems.Should().Be(1);
        result.Value.Progress.TotalPoints.Should().Be(18);
        result.Value.Progress.DonePoints.Should().Be(5);
        result.Value.Progress.InProgressPoints.Should().Be(5);
        result.Value.Progress.ToDoPoints.Should().Be(8);
    }

    [Fact]
    public async Task Handle_PastReleaseDate_IsOverdueTrue()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New()
            .WithProject(project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseDetailQuery(release.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReleasedStatus_IsOverdueFalse()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New()
            .WithProject(project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .Released()
            .Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseDetailQuery(release.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FutureReleaseDate_IsOverdueFalse()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New()
            .WithProject(project.Id)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseDetailQuery(release.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        var result = await Sender.Send(new GetReleaseDetailQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("Releases")]
public sealed class GetReleaseDetailDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseDetailQuery(release.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
