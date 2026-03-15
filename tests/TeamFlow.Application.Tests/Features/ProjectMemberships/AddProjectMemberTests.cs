using CSharpFunctionalExtensions;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships;
using TeamFlow.Application.Features.ProjectMemberships.AddProjectMember;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

public sealed class AddProjectMemberTests
{
    private readonly IProjectMembershipRepository _membershipRepo = Substitute.For<IProjectMembershipRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public AddProjectMemberTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _membershipRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _membershipRepo.AddAsync(Arg.Any<ProjectMembership>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<ProjectMembership>());
    }

    private AddProjectMemberHandler CreateHandler() =>
        new(_membershipRepo, _projectRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_AddUser_ReturnsSuccessWithDto()
    {
        var projectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = ProjectBuilder.New().Build();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var cmd = new AddProjectMemberCommand(projectId, memberId, "User", ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MemberId.Should().Be(memberId);
        result.Value.MemberType.Should().Be("User");
        result.Value.Role.Should().Be(ProjectRole.Developer);
    }

    [Fact]
    public async Task Handle_AddTeam_ReturnsSuccessWithDto()
    {
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var project = ProjectBuilder.New().Build();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var cmd = new AddProjectMemberCommand(projectId, teamId, "Team", ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MemberType.Should().Be("Team");
    }

    [Fact]
    public async Task Handle_DuplicateMember_ReturnsConflict()
    {
        var projectId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var project = ProjectBuilder.New().Build();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _membershipRepo.ExistsAsync(projectId, memberId, "User", Arg.Any<CancellationToken>())
            .Returns(true);

        var cmd = new AddProjectMemberCommand(projectId, memberId, "User", ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns((Project?)null);

        var cmd = new AddProjectMemberCommand(projectId, Guid.NewGuid(), "User", ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var project = ProjectBuilder.New().Build();
        _projectRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(project);

        var cmd = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "User", ProjectRole.Developer);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("user")]
    public async Task Validate_InvalidMemberType_ReturnsValidationError(string memberType)
    {
        var validator = new AddProjectMemberValidator();
        var cmd = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), memberType, ProjectRole.Developer);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddProjectMemberCommand.MemberType));
    }

    [Fact]
    public async Task Validate_ValidMemberTypes_Pass()
    {
        var validator = new AddProjectMemberValidator();

        var userResult = await validator.ValidateAsync(
            new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "User", ProjectRole.Developer));
        var teamResult = await validator.ValidateAsync(
            new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "Team", ProjectRole.Developer));

        userResult.IsValid.Should().BeTrue();
        teamResult.IsValid.Should().BeTrue();
    }
}
