using CSharpFunctionalExtensions;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.UpdateProject;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

public sealed class UpdateProjectTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public UpdateProjectTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UpdateProjectHandler CreateHandler() =>
        new(_projectRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesProject()
    {
        var project = ProjectBuilder.New().WithName("Old Name").Build();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _projectRepo.UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Project>());

        var cmd = new UpdateProjectCommand(project.Id, "New Name", "New desc");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Description.Should().Be("New desc");
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns((Project?)null);

        var cmd = new UpdateProjectCommand(projectId, "New Name", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var cmd = new UpdateProjectCommand(Guid.NewGuid(), "New Name", null);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
        await _projectRepo.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task Handle_PermissionCheckedWithProjectEditPermission()
    {
        var project = ProjectBuilder.New().Build();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _projectRepo.UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Project>());

        await CreateHandler().Handle(new UpdateProjectCommand(project.Id, "Name", null), CancellationToken.None);

        await _permissions.Received(1)
            .HasPermissionAsync(_currentUser.Id, project.Id, Permission.Project_Edit, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new UpdateProjectValidator();
        var cmd = new UpdateProjectCommand(Guid.NewGuid(), name!, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}
