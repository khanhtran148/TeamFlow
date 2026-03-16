using FluentAssertions;
using TeamFlow.Application.Features.Releases.GetRelease;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class GetReleaseTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingRelease_ReturnsReleaseDto()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).WithName("v1.0.0").WithDescription("First release").Build();
        DbContext.Releases.Add(release);
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.Done));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseQuery(release.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("v1.0.0");
        result.Value.ItemCountsByStatus.Should().ContainKey("Done");
    }

    [Fact]
    public async Task Handle_NonExistentRelease_ReturnsFailure()
    {
        var result = await Sender.Send(new GetReleaseQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
