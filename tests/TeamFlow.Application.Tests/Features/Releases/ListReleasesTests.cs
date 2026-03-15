using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.ListReleases;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class ListReleasesTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();

    private ListReleasesHandler CreateHandler() => new(_releaseRepo);

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedReleases()
    {
        var projectId = Guid.NewGuid();
        var releases = new[]
        {
            ReleaseBuilder.New().WithProject(projectId).WithName("v1.0").Build(),
            ReleaseBuilder.New().WithProject(projectId).WithName("v2.0").Build()
        };
        _releaseRepo.ListByProjectAsync(projectId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((releases.AsEnumerable(), 2));

        var result = await CreateHandler().Handle(
            new ListReleasesQuery(projectId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyPage()
    {
        var projectId = Guid.NewGuid();
        _releaseRepo.ListByProjectAsync(projectId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Release>(), 0));

        var result = await CreateHandler().Handle(
            new ListReleasesQuery(projectId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
