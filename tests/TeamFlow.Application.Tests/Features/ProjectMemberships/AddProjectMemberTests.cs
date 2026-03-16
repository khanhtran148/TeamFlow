using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.AddProjectMember;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

[Collection("Projects")]
public sealed class AddProjectMemberTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_AddUser_ReturnsSuccessWithDto()
    {
        var project = await SeedProjectAsync();
        var memberId = Guid.NewGuid();

        var cmd = new AddProjectMemberCommand(project.Id, memberId, "User", ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.MemberId.Should().Be(memberId);
        result.Value.MemberType.Should().Be("User");
        result.Value.Role.Should().Be(ProjectRole.Developer);
    }

    [Fact]
    public async Task Handle_AddTeam_ReturnsSuccessWithDto()
    {
        var project = await SeedProjectAsync();
        var teamId = Guid.NewGuid();

        var cmd = new AddProjectMemberCommand(project.Id, teamId, "Team", ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.MemberType.Should().Be("Team");
    }

    [Fact]
    public async Task Handle_DuplicateMember_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var memberId = Guid.NewGuid();

        var membership = ProjectMembershipBuilder.New()
            .WithProject(project.Id)
            .WithMember(memberId)
            .WithMemberType("User")
            .Build();
        DbContext.ProjectMemberships.Add(membership);
        await DbContext.SaveChangesAsync();

        var cmd = new AddProjectMemberCommand(project.Id, memberId, "User", ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ReturnsNotFound()
    {
        var cmd = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "User", ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
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

[Collection("Projects")]
public sealed class AddProjectMemberForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var cmd = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "User", ProjectRole.Developer);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
