using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.ListSprints;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class ListSprintsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ReturnsPaginatedSprints()
    {
        var project = await SeedProjectAsync();
        var sprint1 = SprintBuilder.New().WithProject(project.Id).WithName("Sprint 1").Build();
        var sprint2 = SprintBuilder.New().WithProject(project.Id).WithName("Sprint 2").Build();
        DbContext.Sprints.AddRange(sprint1, sprint2);
        await DbContext.SaveChangesAsync();

        var query = new ListSprintsQuery(project.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }
}

[Collection("Sprints")]
public sealed class ListSprintsDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var query = new ListSprintsQuery(project.Id);
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
