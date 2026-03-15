using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class CreateSprintTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public CreateSprintTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _sprintRepo.AddAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sprint>());
    }

    private CreateSprintHandler CreateHandler() =>
        new(_sprintRepo, _projectRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_CreatesSprintInPlanningStatus()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateSprintCommand(projectId, "Sprint 1", "Deliver MVP", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sprint 1");
        result.Value.Goal.Should().Be("Deliver MVP");
        result.Value.Status.Should().Be(SprintStatus.Planning);
        result.Value.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Handle_WithDates_CreatesSprintWithDates()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);

        var start = new DateOnly(2026, 3, 16);
        var end = new DateOnly(2026, 3, 30);
        var cmd = new CreateSprintCommand(projectId, "Sprint 1", null, start, end);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(start);
        result.Value.EndDate.Should().Be(end);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateSprintCommand(projectId, "Sprint 1", null, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var projectId = Guid.NewGuid();
        _permissions.HasPermissionAsync(ActorId, projectId, Permission.Sprint_Create, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new CreateSprintCommand(projectId, "Sprint 1", null, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
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
