using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Admin.ListUsers;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ListAdminUsersPagedTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminListUsersHandler CreateHandler() => new(_userRepo, _currentUser);

    private void SetupSystemAdmin() =>
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResult()
    {
        SetupSystemAdmin();
        var users = Enumerable.Range(1, 5)
            .Select(i => UserBuilder.New().WithEmail($"user{i}@example.com").Build())
            .ToList();
        _userRepo.ListPagedAsync(null, 1, 3, Arg.Any<CancellationToken>())
            .Returns((users.Take(3).ToList() as IEnumerable<User>, 5));
        var query = new AdminListUsersQuery(null, 1, 3);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(3);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SearchByName_FiltersResults()
    {
        SetupSystemAdmin();
        var alice = UserBuilder.New().WithName("Alice Smith").WithEmail("alice@example.com").Build();
        _userRepo.ListPagedAsync("alice", 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<User> { alice } as IEnumerable<User>, 1));
        var query = new AdminListUsersQuery("alice", 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.Single().Name.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task Handle_EmptySearch_ReturnsAll()
    {
        SetupSystemAdmin();
        var users = new List<User>
        {
            UserBuilder.New().WithEmail("a@example.com").Build(),
            UserBuilder.New().WithEmail("b@example.com").Build(),
        };
        _userRepo.ListPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((users as IEnumerable<User>, 2));
        var query = new AdminListUsersQuery(null, 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UserDto_IncludesIsActiveAndMustChangePassword()
    {
        SetupSystemAdmin();
        var user = UserBuilder.New()
            .WithIsActive(false)
            .WithMustChangePassword(true)
            .Build();
        _userRepo.ListPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<User> { user } as IEnumerable<User>, 1));
        var query = new AdminListUsersQuery(null, 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items.Single();
        dto.IsActive.Should().BeFalse();
        dto.MustChangePassword.Should().BeTrue();
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var query = new AdminListUsersQuery(null, 1, 20);

        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
