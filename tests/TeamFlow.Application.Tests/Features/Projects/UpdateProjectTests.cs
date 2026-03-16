using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.UpdateProject;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

[Collection("Projects")]
public sealed class UpdateProjectTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_UpdatesProject()
    {
        var project = await SeedProjectAsync(b => b.WithName("Old Name"));

        var cmd = new UpdateProjectCommand(project.Id, "New Name", "New desc");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Description.Should().Be("New desc");
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var cmd = new UpdateProjectCommand(Guid.NewGuid(), "New Name", null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new UpdateProjectValidator();
        var cmd = new UpdateProjectCommand(Guid.NewGuid(), name!, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}

[Collection("Projects")]
public sealed class UpdateProjectForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var cmd = new UpdateProjectCommand(Guid.NewGuid(), "New Name", null);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
