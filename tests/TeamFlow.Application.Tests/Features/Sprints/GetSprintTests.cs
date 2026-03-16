using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.GetSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class GetSprintTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingSprint_ReturnsDetailDto()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithName("Sprint 1")
            .WithGoal("Deliver feature X")
            .Build();
        DbContext.Sprints.Add(sprint);
        await SeedWorkItemAsync(project.Id, b => b
            .WithSprint(sprint.Id)
            .WithEstimation(5)
            .WithStatus(WorkItemStatus.Done));
        await DbContext.SaveChangesAsync();

        var query = new GetSprintQuery(sprint.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sprint 1");
        result.Value.Goal.Should().Be("Deliver feature X");
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalPoints.Should().Be(5);
        result.Value.CompletedPoints.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NonExistentSprint_ReturnsNotFoundError()
    {
        var query = new GetSprintQuery(Guid.NewGuid());
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}

[Collection("Sprints")]
public sealed class GetSprintDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var query = new GetSprintQuery(sprint.Id);
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
