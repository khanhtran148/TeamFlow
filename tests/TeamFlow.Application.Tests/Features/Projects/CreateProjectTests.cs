using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

[Collection("Projects")]
public sealed class CreateProjectTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithProjectDto()
    {
        var cmd = new CreateProjectCommand(SeedOrgId, "My Project", "Some description");

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Project");
        result.Value.OrgId.Should().Be(SeedOrgId);
        result.Value.Status.Should().Be("Active");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new CreateProjectValidator();
        var cmd = new CreateProjectCommand(SeedOrgId, name!, null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectCommand.Name));
    }

    [Fact]
    public async Task Handle_EmptyOrgId_ReturnsValidationError()
    {
        var validator = new CreateProjectValidator();
        var cmd = new CreateProjectCommand(Guid.Empty, "My Project", null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}

[Collection("Projects")]
public sealed class CreateProjectForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var cmd = new CreateProjectCommand(SeedOrgId, "My Project", null);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
