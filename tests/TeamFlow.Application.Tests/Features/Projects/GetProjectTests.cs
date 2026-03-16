using FluentAssertions;
using TeamFlow.Application.Features.Projects.GetProject;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

[Collection("Projects")]
public sealed class GetProjectTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingProject_ReturnsProjectDto()
    {
        var project = await SeedProjectAsync(b => b.WithName("Alpha"));

        var result = await Sender.Send(new GetProjectQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task Handle_NonExistentProject_ReturnsNotFound()
    {
        var result = await Sender.Send(new GetProjectQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Project not found");
    }
}
