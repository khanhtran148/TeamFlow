using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.CreateRelease;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class CreateReleaseTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesRelease()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateReleaseCommand(project.Id, "v1.0.0", "First release", null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("v1.0.0");
        result.Value.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task Handle_MissingProject_ReturnsNotFound()
    {
        var cmd = new CreateReleaseCommand(Guid.NewGuid(), "v1.0.0", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_Fails(string? name)
    {
        var validator = new CreateReleaseValidator();
        var cmd = new CreateReleaseCommand(Guid.NewGuid(), name!, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}

[Collection("Releases")]
public sealed class CreateReleaseDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateReleaseCommand(project.Id, "v1.0.0", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
