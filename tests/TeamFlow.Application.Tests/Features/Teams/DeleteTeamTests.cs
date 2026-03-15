using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.DeleteTeam;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class DeleteTeamTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public DeleteTeamTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private DeleteTeamHandler CreateHandler() => new(_teamRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ExistingTeam_DeletesSuccessfully()
    {
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await CreateHandler().Handle(new DeleteTeamCommand(team.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _teamRepo.Received(1).DeleteAsync(team, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _teamRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Team?)null);

        var result = await CreateHandler().Handle(new DeleteTeamCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _permissions.HasPermissionAsync(ActorId, team.OrgId, Permission.Team_Manage, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new DeleteTeamCommand(team.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
