using CSharpFunctionalExtensions;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Application.Features.Teams.CreateTeam;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class CreateTeamTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public CreateTeamTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _teamRepo.AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Team>());
    }

    private CreateTeamHandler CreateHandler() => new(_teamRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithTeamDto()
    {
        var orgId = Guid.NewGuid();
        var cmd = new CreateTeamCommand(orgId, "Backend Team", "Backend engineers");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Backend Team");
        result.Value.OrgId.Should().Be(orgId);
        result.Value.Description.Should().Be("Backend engineers");
        result.Value.MemberCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var cmd = new CreateTeamCommand(Guid.NewGuid(), "Backend Team", null);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
        await _teamRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_PermissionCheckedAgainstTeamManageAndOrgId()
    {
        var orgId = Guid.NewGuid();
        var cmd = new CreateTeamCommand(orgId, "Backend Team", null);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _permissions.Received(1)
            .HasPermissionAsync(_currentUser.Id, orgId, Permission.Team_Manage, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new CreateTeamValidator();
        var cmd = new CreateTeamCommand(Guid.NewGuid(), name!, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.Name));
    }

    [Fact]
    public async Task Validate_EmptyOrgId_ReturnsValidationError()
    {
        var validator = new CreateTeamValidator();
        var cmd = new CreateTeamCommand(Guid.Empty, "Backend Team", null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTeamCommand.OrgId));
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        var validator = new CreateTeamValidator();
        var cmd = new CreateTeamCommand(Guid.NewGuid(), new string('x', 101), null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}
