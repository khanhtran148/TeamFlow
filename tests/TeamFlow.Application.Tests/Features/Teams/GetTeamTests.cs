using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.GetTeam;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class GetTeamTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();

    private GetTeamHandler CreateHandler() => new(_teamRepo);

    [Fact]
    public async Task Handle_ExistingTeam_ReturnsTeamDto()
    {
        var team = TeamBuilder.New().WithName("Alpha Team").WithDescription("The best team").Build();
        _teamRepo.GetByIdWithMembersAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await CreateHandler().Handle(new GetTeamQuery(team.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Alpha Team");
        result.Value.Description.Should().Be("The best team");
    }

    [Fact]
    public async Task Handle_NonExistentTeam_ReturnsFailure()
    {
        var teamId = Guid.NewGuid();
        _teamRepo.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>()).Returns((Domain.Entities.Team?)null);

        var result = await CreateHandler().Handle(new GetTeamQuery(teamId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
