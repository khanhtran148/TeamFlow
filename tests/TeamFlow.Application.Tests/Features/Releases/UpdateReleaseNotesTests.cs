using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.UpdateReleaseNotes;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class UpdateReleaseNotesTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public UpdateReleaseNotesTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _releaseRepo.UpdateAsync(Arg.Any<Release>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Release>());
    }

    private UpdateReleaseNotesHandler CreateHandler() =>
        new(_releaseRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidRelease_UpdatesNotes()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var result = await CreateHandler().Handle(
            new UpdateReleaseNotesCommand(release.Id, "## Changes\n- Feature A"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        release.ReleaseNotes.Should().Be("## Changes\n- Feature A");
    }

    [Fact]
    public async Task Handle_NotesLocked_ReturnsFailure()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).WithNotesLocked().Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var result = await CreateHandler().Handle(
            new UpdateReleaseNotesCommand(release.Id, "New notes"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Release_Edit, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new UpdateReleaseNotesCommand(release.Id, "Notes"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
