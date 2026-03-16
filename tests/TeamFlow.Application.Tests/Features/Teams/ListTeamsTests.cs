using FluentAssertions;
using TeamFlow.Application.Features.Teams.ListTeams;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class ListTeamsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidQuery_ReturnsPaginatedTeams()
    {
        var team1 = TeamBuilder.New().WithOrganization(SeedOrgId).WithName("Alpha").Build();
        var team2 = TeamBuilder.New().WithOrganization(SeedOrgId).WithName("Beta").Build();
        DbContext.Teams.AddRange(team1, team2);
        await DbContext.SaveChangesAsync();

        var query = new ListTeamsQuery(SeedOrgId, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Value.Items.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_EmptyOrg_ReturnsEmptyList()
    {
        var emptyOrg = OrganizationBuilder.New().Build();
        DbContext.Organizations.Add(emptyOrg);
        await DbContext.SaveChangesAsync();

        var query = new ListTeamsQuery(emptyOrg.Id, 1, 20);
        var result = await Sender.Send(query);

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
