using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.UpdateSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class UpdateSprintTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public UpdateSprintTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _sprintRepo.UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sprint>());
    }

    private UpdateSprintHandler CreateHandler() =>
        new(_sprintRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_PlanningSprintUpdate_Succeeds()
    {
        var sprint = SprintBuilder.New()
            .WithName("Old Name")
            .WithStatus(SprintStatus.Planning)
            .Build();

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new UpdateSprintCommand(sprint.Id, "New Name", "New Goal", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Goal.Should().Be("New Goal");
    }

    [Fact]
    public async Task Handle_ActiveSprint_ReturnsError()
    {
        var sprint = SprintBuilder.New().Active().Build();
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new UpdateSprintCommand(sprint.Id, "New Name", null, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not in Planning status");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        _sprintRepo.GetByIdWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var cmd = new UpdateSprintCommand(Guid.NewGuid(), "Name", null, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var sprint = SprintBuilder.New().WithStatus(SprintStatus.Planning).Build();
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _permissions.HasPermissionAsync(ActorId, sprint.ProjectId, Permission.Sprint_Edit, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new UpdateSprintCommand(sprint.Id, "Name", null, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
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
