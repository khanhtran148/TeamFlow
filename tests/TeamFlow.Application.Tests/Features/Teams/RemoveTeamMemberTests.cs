using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.RemoveTeamMember;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class RemoveTeamMemberTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public RemoveTeamMemberTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private RemoveTeamMemberHandler CreateHandler() => new(_teamRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ExistingMember_RemovesSuccessfully()
    {
        var userId = Guid.NewGuid();
        var team = TeamBuilder.New().WithMember(userId, ProjectRole.Developer).Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await CreateHandler().Handle(
            new RemoveTeamMemberCommand(team.Id, userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        team.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _teamRepo.GetByIdWithMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Team?)null);

        var result = await CreateHandler().Handle(
            new RemoveTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_MemberNotInTeam_ReturnsFailure()
    {
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await CreateHandler().Handle(
            new RemoveTeamMemberCommand(team.Id, Guid.NewGuid()), CancellationToken.None);

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

        var result = await CreateHandler().Handle(
            new RemoveTeamMemberCommand(team.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
