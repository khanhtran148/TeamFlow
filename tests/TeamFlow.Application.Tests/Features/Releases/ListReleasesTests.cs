using FluentAssertions;
using TeamFlow.Application.Features.Releases.ListReleases;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class ListReleasesTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedReleases()
    {
        var project = await SeedProjectAsync();
        DbContext.Releases.Add(ReleaseBuilder.New().WithProject(project.Id).WithName("v1.0").Build());
        DbContext.Releases.Add(ReleaseBuilder.New().WithProject(project.Id).WithName("v2.0").Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListReleasesQuery(project.Id, 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyPage()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListReleasesQuery(project.Id, 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
