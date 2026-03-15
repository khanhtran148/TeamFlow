using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.DeleteRelease;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class DeleteReleaseTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public DeleteReleaseTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private DeleteReleaseHandler CreateHandler() => new(_releaseRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_UnreleasedRelease_Deletes()
    {
        var release = ReleaseBuilder.New().WithStatus(ReleaseStatus.Unreleased).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var result = await CreateHandler().Handle(new DeleteReleaseCommand(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _releaseRepo.Received(1).UnlinkAllItemsAsync(release.Id, Arg.Any<CancellationToken>());
        await _releaseRepo.Received(1).DeleteAsync(release.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReleasedRelease_ReturnsError()
    {
        var release = ReleaseBuilder.New().Released().Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var result = await CreateHandler().Handle(new DeleteReleaseCommand(release.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("released release");
    }

    [Fact]
    public async Task Handle_NonExistentRelease_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _releaseRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Release?)null);

        var result = await CreateHandler().Handle(new DeleteReleaseCommand(id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
