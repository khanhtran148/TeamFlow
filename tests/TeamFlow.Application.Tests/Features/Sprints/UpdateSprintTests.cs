using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.UpdateSprint;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class UpdateSprintTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_PlanningSprintUpdate_Succeeds()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New()
            .WithProject(project.Id)
            .WithName("Old Name")
            .WithStatus(SprintStatus.Planning)
            .Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateSprintCommand(sprint.Id, "New Name", "New Goal", null, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Goal.Should().Be("New Goal");
    }

    [Fact]
    public async Task Handle_ActiveSprint_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Active().Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateSprintCommand(sprint.Id, "New Name", null, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not in Planning status");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        var cmd = new UpdateSprintCommand(Guid.NewGuid(), "Name", null, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_Fails(string? name)
    {
        var validator = new UpdateSprintValidator();
        var cmd = new UpdateSprintCommand(Guid.NewGuid(), name!, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptySprintId_Fails()
    {
        var validator = new UpdateSprintValidator();
        var cmd = new UpdateSprintCommand(Guid.Empty, "Sprint 1", null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NameTooLong_Fails()
    {
        var validator = new UpdateSprintValidator();
        var cmd = new UpdateSprintCommand(Guid.NewGuid(), new string('A', 101), null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EndDateBeforeStartDate_Fails()
    {
        var validator = new UpdateSprintValidator();
        var cmd = new UpdateSprintCommand(
            Guid.NewGuid(), "Sprint 1", null,
            new DateOnly(2026, 3, 30), new DateOnly(2026, 3, 16));
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var validator = new UpdateSprintValidator();
        var cmd = new UpdateSprintCommand(
            Guid.NewGuid(), "Sprint 1", "Goal",
            new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30));
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Sprints")]
public sealed class UpdateSprintDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateSprintCommand(sprint.Id, "Name", null, null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
