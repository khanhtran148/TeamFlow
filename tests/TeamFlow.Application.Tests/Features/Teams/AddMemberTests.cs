using CSharpFunctionalExtensions;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Application.Features.Teams.AddTeamMember;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class AddMemberTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public AddMemberTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private AddTeamMemberHandler CreateHandler() => new(_teamRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var orgId = Guid.NewGuid();
        var team = TeamBuilder.New().WithOrganization(orgId).Build();
        var userId = Guid.NewGuid();

        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _teamRepo.UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Team>());

        var cmd = new AddTeamMemberCommand(team.Id, userId, ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        team.Members.Should().ContainSingle(m => m.UserId == userId);
    }

    [Fact]
    public async Task Handle_DuplicateMember_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var team = TeamBuilder.New().WithMember(userId).Build();

        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var cmd = new AddTeamMemberCommand(team.Id, userId, ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsNotFound()
    {
        var teamId = Guid.NewGuid();
        _teamRepo.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>()).Returns((Team?)null);

        var cmd = new AddTeamMemberCommand(teamId, Guid.NewGuid(), ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var teamId = Guid.NewGuid();
        var team = TeamBuilder.New().Build();
        _teamRepo.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>()).Returns(team);

        var cmd = new AddTeamMemberCommand(teamId, Guid.NewGuid(), ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task Validate_EmptyUserId_ReturnsValidationError()
    {
        var validator = new AddTeamMemberValidator();
        var cmd = new AddTeamMemberCommand(Guid.NewGuid(), Guid.Empty, ProjectRole.Developer);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddTeamMemberCommand.UserId));
    }

    [Fact]
    public async Task Validate_EmptyTeamId_ReturnsValidationError()
    {
        var validator = new AddTeamMemberValidator();
        var cmd = new AddTeamMemberCommand(Guid.Empty, Guid.NewGuid(), ProjectRole.Developer);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddTeamMemberCommand.TeamId));
    }
}
