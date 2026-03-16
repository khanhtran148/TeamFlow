using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.ArchiveProject;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

[Collection("Projects")]
public sealed class ArchiveProjectTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ActiveProject_SetsStatusToArchived()
    {
        var project = await SeedProjectAsync(b => b.WithStatus("Active"));

        var result = await Sender.Send(new ArchiveProjectCommand(project.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Projects.FindAsync(project.Id);
        updated!.Status.Should().Be("Archived");
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var result = await Sender.Send(new ArchiveProjectCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }
}

[Collection("Projects")]
public sealed class ArchiveProjectForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var result = await Sender.Send(new ArchiveProjectCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
