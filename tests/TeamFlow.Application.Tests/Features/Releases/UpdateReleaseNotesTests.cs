using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.UpdateReleaseNotes;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class UpdateReleaseNotesTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidRelease_UpdatesNotes()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new UpdateReleaseNotesCommand(release.Id, "## Changes\n- Feature A"));

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Releases.FindAsync(release.Id);
        updated!.ReleaseNotes.Should().Be("## Changes\n- Feature A");
    }

    [Fact]
    public async Task Handle_NotesLocked_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).WithNotesLocked().Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new UpdateReleaseNotesCommand(release.Id, "New notes"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("locked");
    }
}

[Collection("Releases")]
public sealed class UpdateReleaseNotesDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new UpdateReleaseNotesCommand(release.Id, "Notes"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
