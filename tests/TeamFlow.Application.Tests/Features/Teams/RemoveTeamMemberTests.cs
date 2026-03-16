using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.RemoveTeamMember;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class RemoveTeamMemberTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingMember_RemovesSuccessfully()
    {
        var memberUser = UserBuilder.New().WithEmail("removeteam-member@example.com").Build();
        DbContext.Users.Add(memberUser);
        await DbContext.SaveChangesAsync();

        var userId = memberUser.Id;
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).WithMember(userId, ProjectRole.Developer).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RemoveTeamMemberCommand(team.Id, userId));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        DbContext.Entry(await DbContext.Teams.FindAsync(team.Id)!).Collection(t => t.Members).Load();
        var updatedTeam = await DbContext.Teams.FindAsync(team.Id);
        DbContext.Entry(updatedTeam!).Collection(t => t.Members).Load();
        updatedTeam!.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        var result = await Sender.Send(new RemoveTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_MemberNotInTeam_ReturnsFailure()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RemoveTeamMemberCommand(team.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("member not found");
    }
}

[Collection("Projects")]
public sealed class RemoveTeamMemberForbiddenTests(PostgresCollectionFixture fixture)
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

        var result = await Sender.Send(new RemoveTeamMemberCommand(team.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
