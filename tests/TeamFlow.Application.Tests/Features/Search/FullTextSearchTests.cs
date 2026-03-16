using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Search.FullTextSearch;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Search;

[Collection("WorkItems")]
public sealed class FullTextSearchTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedResults()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithTitle("A searchable task"));

        var query = new FullTextSearchQuery(project.Id, "searchable", null, null, null, null, null, null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyPage()
    {
        var project = await SeedProjectAsync();

        var query = new FullTextSearchQuery(project.Id, "zzznomatchwhatsoever", null, null, null, null, null, null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}

[Collection("WorkItems")]
public sealed class FullTextSearchPermissionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Application.Common.Interfaces.IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var query = new FullTextSearchQuery(project.Id, "test", null, null, null, null, null, null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
