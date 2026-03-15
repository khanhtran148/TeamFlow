using CSharpFunctionalExtensions;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Application.Features.Teams.ListTeams;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

public sealed class ListTeamsTests
{
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();

    private ListTeamsHandler CreateHandler() => new(_teamRepo);

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPaginatedTeams()
    {
        var orgId = Guid.NewGuid();
        var teams = new[]
        {
            TeamBuilder.New().WithOrganization(orgId).WithName("Alpha").Build(),
            TeamBuilder.New().WithOrganization(orgId).WithName("Beta").Build()
        };

        _teamRepo.ListByOrgAsync(orgId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((teams.AsEnumerable(), 2));

        var query = new ListTeamsQuery(orgId, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyOrg_ReturnsEmptyList()
    {
        var orgId = Guid.NewGuid();
        _teamRepo.ListByOrgAsync(orgId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Team>(), 0));

        var query = new ListTeamsQuery(orgId, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Validate_EmptyOrgId_ReturnsValidationError()
    {
        var validator = new ListTeamsValidator();
        var query = new ListTeamsQuery(Guid.Empty, 1, 20);

        var result = await validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ListTeamsQuery.OrgId));
    }
}
