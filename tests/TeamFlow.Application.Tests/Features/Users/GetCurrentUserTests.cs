using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Users.GetCurrentUser;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Users;

public sealed class GetCurrentUserTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetCurrentUserTests()
    {
        _currentUser.Id.Returns(UserId);
    }

    private GetCurrentUserHandler CreateHandler() =>
        new(_currentUser, _userRepo, _orgRepo);

    [Fact]
    public async Task Handle_ValidUser_ReturnsCurrentUserWithOrganizations()
    {
        var user = new User { Email = "test@example.com", Name = "Test User" };
        var orgs = new List<Organization>
        {
            new() { Name = "Org A" },
            new() { Name = "Org B" }
        };

        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _orgRepo.ListByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(orgs);

        var result = await CreateHandler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Name.Should().Be("Test User");
        result.Value.Organizations.Should().HaveCount(2);
        result.Value.Organizations[0].OrgName.Should().Be("Org A");
        result.Value.Organizations[1].OrgName.Should().Be("Org B");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_UserWithNoOrganizations_ReturnsEmptyOrgList()
    {
        var user = new User { Email = "solo@example.com", Name = "Solo User" };
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _orgRepo.ListByUserAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Organization>());

        var result = await CreateHandler().Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Organizations.Should().BeEmpty();
    }
}
