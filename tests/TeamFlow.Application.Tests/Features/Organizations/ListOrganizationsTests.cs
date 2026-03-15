using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.ListOrganizations;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Organizations;

public sealed class ListOrganizationsTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public ListOrganizationsTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
    }

    private ListOrganizationsHandler CreateHandler() =>
        new(_orgRepo, _currentUser);

    [Fact]
    public async Task Handle_UserHasOrganizations_ReturnsMappedDtos()
    {
        var orgs = new List<Organization>
        {
            new() { Name = "Org A" },
            new() { Name = "Org B" }
        };
        _orgRepo.ListByUserAsync(_currentUser.Id, Arg.Any<CancellationToken>())
            .Returns(orgs);

        var result = await CreateHandler().Handle(new ListOrganizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var items = result.Value.ToList();
        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Org A");
        items[1].Name.Should().Be("Org B");
    }

    [Fact]
    public async Task Handle_UserHasNoOrganizations_ReturnsEmptyList()
    {
        _orgRepo.ListByUserAsync(_currentUser.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Organization>());

        var result = await CreateHandler().Handle(new ListOrganizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_QueriesForCurrentUser()
    {
        _orgRepo.ListByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Organization>());

        await CreateHandler().Handle(new ListOrganizationsQuery(), CancellationToken.None);

        await _orgRepo.Received(1).ListByUserAsync(_currentUser.Id, Arg.Any<CancellationToken>());
    }
}
