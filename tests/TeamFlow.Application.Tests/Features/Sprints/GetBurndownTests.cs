using FluentAssertions;
using TeamFlow.Application.Features.Sprints.GetBurndown;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class GetBurndownTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_SprintWithDataPoints_ReturnsBurndownData()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 20))
            .Active()
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b
            .WithSprint(sprint.Id)
            .WithEstimation(10));

        var dataPoint = BurndownDataPointBuilder.New()
            .WithSprint(sprint.Id)
            .WithDate(new DateOnly(2026, 3, 16))
            .WithRemainingPoints(8)
            .WithCompletedPoints(2)
            .Build();
        DbContext.BurndownDataPoints.Add(dataPoint);
        await DbContext.SaveChangesAsync();

        var query = new GetBurndownQuery(sprint.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.SprintId.Should().Be(sprint.Id);
        result.Value.ActualLine.Should().HaveCount(1);
        result.Value.IdealLine.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_EmptySprint_ReturnsEmptyArrays()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var query = new GetBurndownQuery(sprint.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActualLine.Should().BeEmpty();
        result.Value.IdealLine.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        var query = new GetBurndownQuery(Guid.NewGuid());
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}
