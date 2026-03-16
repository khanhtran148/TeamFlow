using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class CreateSprintTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesSprintInPlanningStatus()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateSprintCommand(project.Id, "Sprint 1", "Deliver MVP", null, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sprint 1");
        result.Value.Goal.Should().Be("Deliver MVP");
        result.Value.Status.Should().Be(SprintStatus.Planning);
        result.Value.ProjectId.Should().Be(project.Id);
    }

    [Fact]
    public async Task Handle_WithDates_CreatesSprintWithDates()
    {
        var project = await SeedProjectAsync();

        var start = new DateOnly(2026, 3, 16);
        var end = new DateOnly(2026, 3, 30);
        var cmd = new CreateSprintCommand(project.Id, "Sprint 1", null, start, end);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(start);
        result.Value.EndDate.Should().Be(end);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        var cmd = new CreateSprintCommand(Guid.NewGuid(), "Sprint 1", null, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new CreateSprintValidator();
        var cmd = new CreateSprintCommand(Guid.NewGuid(), name!, null, null, null);
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EndDateBeforeStartDate_ReturnsValidationError()
    {
        var validator = new CreateSprintValidator();
        var cmd = new CreateSprintCommand(
            Guid.NewGuid(), "Sprint 1", null,
            new DateOnly(2026, 3, 30), new DateOnly(2026, 3, 16));
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    public async Task Validate_EmptyProjectId_ReturnsValidationError()
    {
        var validator = new CreateSprintValidator();
        var cmd = new CreateSprintCommand(Guid.Empty, "Sprint 1", null, null, null);
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        var validator = new CreateSprintValidator();
        var cmd = new CreateSprintCommand(Guid.NewGuid(), new string('A', 101), null, null, null);
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}

[Collection("Sprints")]
public sealed class CreateSprintDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateSprintCommand(project.Id, "Sprint 1", null, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
