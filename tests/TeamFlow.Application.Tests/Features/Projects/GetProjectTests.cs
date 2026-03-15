using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.GetProject;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

public sealed class GetProjectTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();

    private GetProjectHandler CreateHandler() => new(_projectRepo);

    [Fact]
    public async Task Handle_ExistingProject_ReturnsProjectDto()
    {
        var project = ProjectBuilder.New().WithName("Alpha").Build();
        _projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _projectRepo.CountEpicsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(3);
        _projectRepo.CountOpenWorkItemsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(7);

        var result = await CreateHandler().Handle(new GetProjectQuery(project.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Alpha");
        result.Value.EpicCount.Should().Be(3);
        result.Value.OpenItemCount.Should().Be(7);
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await CreateHandler().Handle(new GetProjectQuery(projectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }
}
