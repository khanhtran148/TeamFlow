using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.CreateRetroSession;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class CreateRetroSessionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesSession()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateRetroSessionCommand(project.Id, null, null, "Public");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(project.Id);
        result.Value.FacilitatorId.Should().Be(SeedUserId);
        result.Value.AnonymityMode.Should().Be("Public");
    }

    [Fact]
    public async Task Handle_InvalidProject_ReturnsNotFound()
    {
        var cmd = new CreateRetroSessionCommand(Guid.NewGuid(), null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    public async Task Validate_InvalidAnonymityMode_Fails(string mode)
    {
        var validator = new CreateRetroSessionValidator();
        var cmd = new CreateRetroSessionCommand(Guid.NewGuid(), null, null, mode);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("Anonymous")]
    public async Task Validate_ValidAnonymityMode_Passes(string mode)
    {
        var validator = new CreateRetroSessionValidator();
        var cmd = new CreateRetroSessionCommand(Guid.NewGuid(), null, null, mode);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Social")]
public sealed class CreateRetroSessionForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateRetroSessionCommand(project.Id, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
