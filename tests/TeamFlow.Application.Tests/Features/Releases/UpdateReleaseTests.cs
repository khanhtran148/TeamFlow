using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.UpdateRelease;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class UpdateReleaseTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public UpdateReleaseTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UpdateReleaseHandler CreateHandler() => new(_releaseRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var release = ReleaseBuilder.New().WithName("old").Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _releaseRepo.UpdateAsync(Arg.Any<Release>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Release>());

        var cmd = new UpdateReleaseCommand(release.Id, "v2.0.0", "Updated", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("v2.0.0");
    }

    [Fact]
    public async Task Handle_NotesLocked_ReturnsError()
    {
        var release = ReleaseBuilder.New().WithNotesLocked(true).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var cmd = new UpdateReleaseCommand(release.Id, "v2.0.0", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_NonExistentRelease_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _releaseRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Release?)null);

        var cmd = new UpdateReleaseCommand(id, "v2.0.0", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Release not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_Fails(string? name)
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), name!, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptyReleaseId_Fails()
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.Empty, "Valid Name", null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NameTooLong_Fails()
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), new string('A', 101), null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), "Valid Name", "Desc", null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
