using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.CreateTeam;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class CreateTeamTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithTeamDto()
    {
        var cmd = new CreateTeamCommand(SeedOrgId, "Backend Team", "Backend engineers");

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Backend Team");
        result.Value.OrgId.Should().Be(SeedOrgId);
        result.Value.Description.Should().Be("Backend engineers");
        result.Value.MemberCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new CreateTeamValidator();
        var cmd = new CreateTeamCommand(Guid.NewGuid(), name!, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.Name));
    }

    [Fact]
    public async Task Validate_EmptyOrgId_ReturnsValidationError()
    {
        var validator = new CreateTeamValidator();
        var cmd = new CreateTeamCommand(Guid.Empty, "Backend Team", null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.OrgId));
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        var validator = new CreateTeamValidator();
        var cmd = new CreateTeamCommand(Guid.NewGuid(), new string('x', 101), null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}

[Collection("Projects")]
public sealed class CreateTeamForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var cmd = new CreateTeamCommand(SeedOrgId, "Backend Team", null);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
