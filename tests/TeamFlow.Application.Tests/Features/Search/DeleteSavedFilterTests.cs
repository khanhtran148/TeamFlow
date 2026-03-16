using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Search.DeleteSavedFilter;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Search;

[Collection("WorkItems")]
public sealed class DeleteSavedFilterTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<(SavedFilter filter, Guid projectId)> SeedOwnedFilterAsync(string name = "Test")
    {
        var project = await SeedProjectAsync();
        var filter = new SavedFilter
        {
            UserId = SeedUserId,
            ProjectId = project.Id,
            Name = name,
            FilterJson = JsonDocument.Parse("{}"),
            IsDefault = false
        };
        DbContext.Set<SavedFilter>().Add(filter);
        await DbContext.SaveChangesAsync();
        return (filter, project.Id);
    }

    [Fact]
    public async Task Handle_OwnFilter_DeletesSuccessfully()
    {
        var (filter, projectId) = await SeedOwnedFilterAsync();

        var result = await Sender.Send(new DeleteSavedFilterCommand(projectId, filter.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var found = await DbContext.Set<SavedFilter>().AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == filter.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OtherUsersFilter_ReturnsForbidden()
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

        var result = await Sender.Send(new DeleteSavedFilterCommand(project.Id, filter.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_FilterNotFound_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new DeleteSavedFilterCommand(project.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("WorkItems")]
public sealed class DeleteSavedFilterPermissionTests(PostgresCollectionFixture fixture)
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

        var result = await Sender.Send(new DeleteSavedFilterCommand(project.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
