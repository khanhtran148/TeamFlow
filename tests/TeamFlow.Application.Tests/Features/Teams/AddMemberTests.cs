using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.AddTeamMember;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class AddMemberTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        var memberUser = UserBuilder.New().WithEmail("addteam-member@example.com").Build();
        DbContext.Users.Add(memberUser);
        await DbContext.SaveChangesAsync();

        var userId = memberUser.Id;
        var cmd = new AddTeamMemberCommand(team.Id, userId, ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Teams.FindAsync(team.Id);
        DbContext.Entry(updated!).Collection(t => t.Members).Load();
        updated!.Members.Should().ContainSingle(m => m.UserId == userId);
    }

    [Fact]
    public async Task Handle_DuplicateMember_ReturnsConflict()
    {
        var memberUser = UserBuilder.New().WithEmail("addteam-dup@example.com").Build();
        DbContext.Users.Add(memberUser);
        await DbContext.SaveChangesAsync();

        var userId = memberUser.Id;
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).WithMember(userId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new AddTeamMemberCommand(team.Id, userId, ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsNotFound()
    {
        var cmd = new AddTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
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

[Collection("Projects")]
public sealed class AddMemberForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new AddTeamMemberCommand(team.Id, Guid.NewGuid(), ProjectRole.Developer);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }
}
