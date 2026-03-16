using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.GetBySlug;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Organizations;

public sealed class GetOrganizationBySlugTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public GetOrganizationBySlugTests()
    {
        _currentUser.Id.Returns(_userId);

        var org = new Organization { Name = "Test Org", Slug = "test-org" };
        // Use Entry to set the Id as integration tests do — but for unit tests,
        // we match IsMemberAsync using Arg.Any<Guid> for simplicity.
        _orgRepo.GetBySlugAsync("test-org", Arg.Any<CancellationToken>()).Returns(org);
    }

    private GetOrganizationBySlugHandler CreateHandler() =>
        new(_orgRepo, _memberRepo, _currentUser);

    [Fact]
    public async Task Handle_MemberOfOrg_ReturnsOrgDto()
    {
        _memberRepo.IsMemberAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>())
            .Returns(true);
        var query = new GetOrganizationBySlugQuery("test-org");

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Org");
        result.Value.Slug.Should().Be("test-org");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.IsMemberAsync(Arg.Any<Guid>(), _userId, Arg.Any<CancellationToken>())
            .Returns(false);
        var query = new GetOrganizationBySlugQuery("test-org");

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("access denied", Exactly.Once(),
            options => options.IgnoringCase());
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        _orgRepo.GetBySlugAsync("nonexistent", Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var query = new GetOrganizationBySlugQuery("nonexistent");

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(),
            options => options.IgnoringCase());
    }
}
