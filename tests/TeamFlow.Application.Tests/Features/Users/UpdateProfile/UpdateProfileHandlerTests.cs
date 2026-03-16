using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Users.UpdateProfile;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Users.UpdateProfile;

public sealed class UpdateProfileHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IOrganizationMemberRepository _orgMemberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ITeamMemberRepository _teamMemberRepo = Substitute.For<ITeamMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public UpdateProfileHandlerTests()
    {
        _orgMemberRepo.ListOrganizationsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<(Domain.Entities.Organization, OrgRole, DateTime)>());
        _teamMemberRepo.ListTeamsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<(Domain.Entities.Team, ProjectRole, DateTime)>());
    }

    private UpdateProfileHandler CreateHandler() =>
        new(_userRepo, _orgMemberRepo, _teamMemberRepo, _currentUser);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameAndAvatar()
    {
        var user = UserBuilder.New().WithName("Old Name").Build();
        _currentUser.Id.Returns(user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new UpdateProfileCommand("New Name", "https://example.com/avatar.jpg");
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _userRepo.Received(1).UpdateAsync(
            Arg.Is<Domain.Entities.User>(u => u.Name == "New Name" && u.AvatarUrl == "https://example.com/avatar.jpg"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUpdatedProfile()
    {
        var user = UserBuilder.New().WithName("Old Name").Build();
        _currentUser.Id.Returns(user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new UpdateProfileCommand("New Name", "https://example.com/avatar.jpg");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public async Task Handle_NullAvatarUrl_ClearsAvatar()
    {
        var user = UserBuilder.New().Build();
        user.AvatarUrl = "https://example.com/old-avatar.jpg";
        _currentUser.Id.Returns(user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var cmd = new UpdateProfileCommand("Same Name", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AvatarUrl.Should().BeNull();
        await _userRepo.Received(1).UpdateAsync(
            Arg.Is<Domain.Entities.User>(u => u.AvatarUrl == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Domain.Entities.User?)null);

        var cmd = new UpdateProfileCommand("New Name", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
