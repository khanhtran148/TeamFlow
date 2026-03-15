using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.ListProjects;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

public sealed class ListProjectsTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public ListProjectsTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
    }

    private ListProjectsHandler CreateHandler() => new(_projectRepo);

    [Fact]
    public async Task Handle_WithProjects_ReturnsPagedResult()
    {
        var projects = new List<Project>
        {
            ProjectBuilder.New().WithName("Project A").Build(),
            ProjectBuilder.New().WithName("Project B").Build()
        };
        _projectRepo.ListAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<string?>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((projects, 2));

        var query = new ListProjectsQuery(null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_FilterByStatus_PassesStatusToRepository()
    {
        _projectRepo.ListAsync(Arg.Any<Guid?>(), "Active", Arg.Any<string?>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Project>(), 0));

        var query = new ListProjectsQuery(null, "Active", null, 1, 20);
        await CreateHandler().Handle(query, CancellationToken.None);

        await _projectRepo.Received(1).ListAsync(null, "Active", null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SearchByName_PassesSearchToRepository()
    {
        _projectRepo.ListAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), "alpha", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<Project>(), 0));

        var query = new ListProjectsQuery(null, null, "alpha", 1, 20);
        await CreateHandler().Handle(query, CancellationToken.None);

        await _projectRepo.Received(1).ListAsync(null, null, "alpha", 1, 20, Arg.Any<CancellationToken>());
    }
}
