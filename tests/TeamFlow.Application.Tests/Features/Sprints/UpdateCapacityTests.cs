using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.UpdateCapacity;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class UpdateCapacityTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public UpdateCapacityTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _sprintRepo.UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sprint>());
    }

    private UpdateCapacityHandler CreateHandler() =>
        new(_sprintRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_PlanningSprintCapacityUpdate_Succeeds()
    {
        var sprint = SprintBuilder.New().WithStatus(SprintStatus.Planning).Build();
        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var memberId = Guid.NewGuid();
        var cmd = new UpdateCapacityCommand(sprint.Id, [new CapacityEntry(memberId, 10)]);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sprint.CapacityJson.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ActiveSprintCapacityUpdate_ReturnsError()
    {
        var sprint = SprintBuilder.New().Active().Build();
        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new UpdateCapacityCommand(sprint.Id, [new CapacityEntry(Guid.NewGuid(), 10)]);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Planning status");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Validate_ZeroOrNegativeCapacity_ReturnsValidationError(int points)
    {
        var validator = new UpdateCapacityValidator();
        var cmd = new UpdateCapacityCommand(Guid.NewGuid(), [new CapacityEntry(Guid.NewGuid(), points)]);
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage.Contains("greater than zero"));
    }

    [Fact]
    public async Task Validate_EmptyCapacityList_ReturnsValidationError()
    {
        var validator = new UpdateCapacityValidator();
        var cmd = new UpdateCapacityCommand(Guid.NewGuid(), []);
        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
