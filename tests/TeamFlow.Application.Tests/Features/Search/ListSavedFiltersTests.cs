using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Search.ListSavedFilters;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Search;

[Collection("WorkItems")]
public sealed class ListSavedFiltersTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<Guid> SeedFilterAsync(Guid projectId, string name, bool isDefault = false)
    {
        var filter = new SavedFilter
        {
            UserId = SeedUserId,
            ProjectId = projectId,
            Name = name,
            FilterJson = JsonDocument.Parse("{}"),
            IsDefault = isDefault
        };
        DbContext.Set<SavedFilter>().Add(filter);
        await DbContext.SaveChangesAsync();
        return filter.Id;
    }

    [Fact]
    public async Task Handle_UserHasSavedFilters_ReturnsFilterList()
    {
        var project = await SeedProjectAsync();
        await SeedFilterAsync(project.Id, "My Bugs");
        await SeedFilterAsync(project.Id, "Sprint Tasks", isDefault: true);

        var result = await Sender.Send(new ListSavedFiltersQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(f => f.Name == "My Bugs");
        var sprintFilter = result.Value.FirstOrDefault(f => f.Name == "Sprint Tasks");
        sprintFilter.Should().NotBeNull();
        sprintFilter!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoSavedFilters_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListSavedFiltersQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}

[Collection("WorkItems")]
public sealed class ListSavedFiltersPermissionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Application.Common.Interfaces.IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NotProjectMember_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListSavedFiltersQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
