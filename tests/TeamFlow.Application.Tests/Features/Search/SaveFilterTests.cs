using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Search.SaveFilter;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Search;

[Collection("WorkItems")]
public sealed class SaveFilterTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesFilter()
    {
        var project = await SeedProjectAsync();
        var filterJson = JsonDocument.Parse("""{"status":["ToDo"]}""");
        var cmd = new SaveFilterCommand(project.Id, "My Filter", filterJson, false);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Filter");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        // Seed existing filter with same name
        var existing = new SavedFilter
        {
            UserId = SeedUserId,
            ProjectId = project.Id,
            Name = "Existing",
            FilterJson = JsonDocument.Parse("{}"),
            IsDefault = false
        };
        DbContext.Set<SavedFilter>().Add(existing);
        await DbContext.SaveChangesAsync();

        var cmd = new SaveFilterCommand(project.Id, "Existing", JsonDocument.Parse("{}"), false);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}

[Collection("WorkItems")]
public sealed class SaveFilterPermissionTests(PostgresCollectionFixture fixture)
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
        var cmd = new SaveFilterCommand(project.Id, "Filter", JsonDocument.Parse("{}"), false);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
