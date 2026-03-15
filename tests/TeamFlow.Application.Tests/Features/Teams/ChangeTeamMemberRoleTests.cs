using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.ChangeTeamMemberRole;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class ChangeTeamMemberRoleTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public ChangeTeamMemberRoleTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ChangeTeamMemberRoleHandler CreateHandler() => new(_teamRepo, _userRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_ChangesRole()
    {
        var userId = Guid.NewGuid();
        var team = TeamBuilder.New().WithMember(userId, ProjectRole.Developer).Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _teamRepo.UpdateAsync(team, Arg.Any<CancellationToken>()).Returns(team);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new User { Name = "John", Email = "john@test.com" });

        var cmd = new ChangeTeamMemberRoleCommand(team.Id, userId, ProjectRole.TechnicalLeader);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(ProjectRole.TechnicalLeader);
        result.Value.UserName.Should().Be("John");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _teamRepo.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);

        var cmd = new ChangeTeamMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Developer);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_MemberNotFound_ReturnsFailure()
    {
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var cmd = new ChangeTeamMemberRoleCommand(team.Id, Guid.NewGuid(), ProjectRole.Developer);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("member not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _permissions.HasPermissionAsync(ActorId, team.OrgId, Permission.Team_Manage, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new ChangeTeamMemberRoleCommand(team.Id, Guid.NewGuid(), ProjectRole.Developer);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
