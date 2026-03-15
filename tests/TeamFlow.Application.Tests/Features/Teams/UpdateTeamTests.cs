using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.UpdateTeam;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class UpdateTeamTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public UpdateTeamTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UpdateTeamHandler CreateHandler() => new(_teamRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTeam()
    {
        var team = TeamBuilder.New().WithName("Old Name").Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _teamRepo.UpdateAsync(team, Arg.Any<CancellationToken>()).Returns(team);

        var cmd = new UpdateTeamCommand(team.Id, "New Name", "New Desc");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _teamRepo.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Team?)null);

        var cmd = new UpdateTeamCommand(Guid.NewGuid(), "Name", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _permissions.HasPermissionAsync(ActorId, team.OrgId, Permission.Team_Manage, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new UpdateTeamCommand(team.Id, "Name", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
