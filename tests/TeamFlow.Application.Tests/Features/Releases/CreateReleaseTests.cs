using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.CreateRelease;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class CreateReleaseTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public CreateReleaseTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _releaseRepo.AddAsync(Arg.Any<Release>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Release>());
    }

    private CreateReleaseHandler CreateHandler() =>
        new(_releaseRepo, _projectRepo, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidCommand_CreatesRelease()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateReleaseCommand(projectId, "v1.0.0", "First release", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("v1.0.0");
        result.Value.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Handle_MissingProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateReleaseCommand(projectId, "v1.0.0", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

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
