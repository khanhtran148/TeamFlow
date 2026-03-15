using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.ArchiveProject;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

public sealed class ArchiveProjectTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public ArchiveProjectTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ArchiveProjectHandler CreateHandler() => new(_projectRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ActiveProject_SetsStatusToArchived()
    {
        var project = ProjectBuilder.New().WithStatus("Active").Build();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _projectRepo.UpdateAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Project>());

        var result = await CreateHandler().Handle(new ArchiveProjectCommand(project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        project.Status.Should().Be("Archived");
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await CreateHandler().Handle(new ArchiveProjectCommand(projectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }
}
