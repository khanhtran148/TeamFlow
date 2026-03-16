using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Search.UpdateSavedFilter;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Search;

[Collection("WorkItems")]
public sealed class UpdateSavedFilterTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<(SavedFilter filter, Guid projectId)> SeedOwnedFilterAsync(string name = "Original")
    {
        var project = await SeedProjectAsync();
        var filter = new SavedFilter
        {
            UserId = SeedUserId,
            ProjectId = project.Id,
            Name = name,
            FilterJson = JsonDocument.Parse("""{"status":"Open"}"""),
            IsDefault = false
        };
        DbContext.Set<SavedFilter>().Add(filter);
        await DbContext.SaveChangesAsync();
        return (filter, project.Id);
    }

    [Fact]
    public async Task Handle_UpdateName_ReturnsUpdatedFilter()
    {
        var (filter, projectId) = await SeedOwnedFilterAsync();

        var cmd = new UpdateSavedFilterCommand(projectId, filter.Id, "Renamed", null, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task Handle_UpdateFilterJson_ReturnsUpdatedFilter()
    {
        var (filter, projectId) = await SeedOwnedFilterAsync();
        var newJson = JsonDocument.Parse("""{"status":"Closed"}""");

        var cmd = new UpdateSavedFilterCommand(projectId, filter.Id, null, newJson, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FilterNotFound_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();

        var cmd = new UpdateSavedFilterCommand(project.Id, Guid.NewGuid(), "New Name", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var otherUser = UserBuilder.New().Build();
        DbContext.Users.Add(otherUser);
        await DbContext.SaveChangesAsync();

        var filter = new SavedFilter
        {
            UserId = otherUser.Id,
            ProjectId = project.Id,
            Name = "Other's Filter",
            FilterJson = JsonDocument.Parse("{}"),
            IsDefault = false
        };
        DbContext.Set<SavedFilter>().Add(filter);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateSavedFilterCommand(project.Id, filter.Id, "Stolen", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var filter1 = new SavedFilter
        {
            UserId = SeedUserId,
            ProjectId = project.Id,
            Name = "Original",
            FilterJson = JsonDocument.Parse("{}"),
            IsDefault = false
        };
        var filter2 = new SavedFilter
        {
            UserId = SeedUserId,
            ProjectId = project.Id,
            Name = "Duplicate",
            FilterJson = JsonDocument.Parse("{}"),
            IsDefault = false
        };
        DbContext.Set<SavedFilter>().AddRange(filter1, filter2);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateSavedFilterCommand(project.Id, filter1.Id, "Duplicate", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}

[Collection("WorkItems")]
public sealed class UpdateSavedFilterPermissionTests(PostgresCollectionFixture fixture)
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

        var cmd = new UpdateSavedFilterCommand(project.Id, Guid.NewGuid(), "Name", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
