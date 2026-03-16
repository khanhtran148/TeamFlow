using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.DeleteProject;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

[Collection("Projects")]
public sealed class DeleteProjectTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingProject_SoftDeletes()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new DeleteProjectCommand(project.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Projects.FindAsync(project.Id);
        updated!.Status.Should().Be("Deleted");
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var result = await Sender.Send(new DeleteProjectCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }
}

[Collection("Projects")]
public sealed class DeleteProjectForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var result = await Sender.Send(new DeleteProjectCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
