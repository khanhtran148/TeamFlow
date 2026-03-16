using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ListOrganizations;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ListAdminOrganizationsTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminListOrganizationsHandler CreateHandler() =>
        new(_orgRepo, _currentUser);

    [Fact]
    public async Task Handle_SystemAdmin_ReturnsAllOrganizations()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        var orgs = new List<Organization>
        {
            OrganizationBuilder.New().WithName("Org A").Build(),
            OrganizationBuilder.New().WithName("Org B").Build(),
        };
        _orgRepo.ListAllPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((orgs as IEnumerable<Organization>, 2));

        var result = await CreateHandler().Handle(new AdminListOrganizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);

        var result = await CreateHandler().Handle(new AdminListOrganizationsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }

    [Fact]
    public async Task Handle_SystemAdmin_EmptyOrgs_ReturnsEmptyList()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        _orgRepo.ListAllPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Organization>(), 0));

        var result = await CreateHandler().Handle(new AdminListOrganizationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}
