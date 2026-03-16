using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Users.GetProfile;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Users.GetProfile;

public sealed class GetProfileHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IOrganizationMemberRepository _orgMemberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ITeamMemberRepository _teamMemberRepo = Substitute.For<ITeamMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public GetProfileHandlerTests()
    {
        _orgMemberRepo.ListOrganizationsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<(Domain.Entities.Organization, OrgRole, DateTime)>());
        _teamMemberRepo.ListTeamsForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<(Domain.Entities.Team, ProjectRole, DateTime)>());
    }

    private GetProfileHandler CreateHandler() =>
        new(_userRepo, _orgMemberRepo, _teamMemberRepo, _currentUser);

    [Fact]
    public async Task Handle_AuthenticatedUser_ReturnsFullProfile()
    {
        var user = UserBuilder.New().WithName("Jane Doe").WithEmail("jane@example.com").Build();
        _currentUser.Id.Returns(user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var org = OrganizationBuilder.New().WithName("Acme Corp").Build();
        var team = TeamBuilder.New().WithName("Backend Squad").Build();

        _orgMemberRepo.ListOrganizationsForUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<(Domain.Entities.Organization, OrgRole, DateTime)>
            {
                (org, OrgRole.Admin, DateTime.UtcNow)
            });
        _teamMemberRepo.ListTeamsForUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<(Domain.Entities.Team, ProjectRole, DateTime)>
            {
                (team, ProjectRole.Developer, DateTime.UtcNow)
            });

        var result = await CreateHandler().Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("jane@example.com");
        result.Value.Name.Should().Be("Jane Doe");
        result.Value.SystemRole.Should().Be("User");
        result.Value.Organizations.Should().HaveCount(1);
        result.Value.Organizations[0].OrgName.Should().Be("Acme Corp");
        result.Value.Organizations[0].Role.Should().Be("Admin");
        result.Value.Teams.Should().HaveCount(1);
        result.Value.Teams[0].TeamName.Should().Be("Backend Squad");
        result.Value.Teams[0].Role.Should().Be("Developer");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Domain.Entities.User?)null);

        var result = await CreateHandler().Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_UserWithNoOrgsOrTeams_ReturnsEmptyCollections()
    {
        var user = UserBuilder.New().Build();
        _currentUser.Id.Returns(user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new GetProfileQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Organizations.Should().BeEmpty();
        result.Value.Teams.Should().BeEmpty();
    }
}
