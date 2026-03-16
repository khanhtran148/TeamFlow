using FluentAssertions;
using TeamFlow.Application.Features.Sprints.UpdateCapacity;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

[Collection("Sprints")]
public sealed class UpdateCapacityTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_PlanningSprintCapacityUpdate_Succeeds()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).WithStatus(SprintStatus.Planning).Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var memberId = Guid.NewGuid();
        var cmd = new UpdateCapacityCommand(sprint.Id, [new CapacityEntry(memberId, 10)]);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Sprints.FindAsync(sprint.Id);
        updated!.CapacityJson.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ActiveSprintCapacityUpdate_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Active().Build();
        DbContext.Sprints.Add(sprint);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateCapacityCommand(sprint.Id, [new CapacityEntry(Guid.NewGuid(), 10)]);
        var result = await Sender.Send(cmd);

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
