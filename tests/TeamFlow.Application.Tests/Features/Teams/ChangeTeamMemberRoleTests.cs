using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.ChangeTeamMemberRole;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class ChangeTeamMemberRoleTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_ChangesRole()
    {
        var user = UserBuilder.New().WithName("John").Build();
        DbContext.Users.Add(user);
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).WithMember(user.Id, ProjectRole.Developer).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeTeamMemberRoleCommand(team.Id, user.Id, ProjectRole.TechnicalLeader);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(ProjectRole.TechnicalLeader);
        result.Value.UserName.Should().Be("John");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        var cmd = new ChangeTeamMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_MemberNotFound_ReturnsFailure()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeTeamMemberRoleCommand(team.Id, Guid.NewGuid(), ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("member not found");
    }
}

[Collection("Projects")]
public sealed class ChangeTeamMemberRoleForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new ChangeTeamMemberRoleCommand(team.Id, Guid.NewGuid(), ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
