using FluentAssertions;
using TeamFlow.Application.Features.Releases.DeleteRelease;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class DeleteReleaseTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_UnreleasedRelease_Deletes()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).WithStatus(ReleaseStatus.Unreleased).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var releaseId = release.Id;
        var result = await Sender.Send(new DeleteReleaseCommand(releaseId));

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Releases.FindAsync(releaseId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReleasedRelease_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Released().Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteReleaseCommand(release.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("released release");
    }

    [Fact]
    public async Task Handle_NonExistentRelease_ReturnsNotFound()
    {
        var result = await Sender.Send(new DeleteReleaseCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }
}
