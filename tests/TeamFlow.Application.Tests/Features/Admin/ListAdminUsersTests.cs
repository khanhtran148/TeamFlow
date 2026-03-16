using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ListUsers;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ListAdminUsersTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminListUsersHandler CreateHandler() =>
        new(_userRepo, _currentUser);

    [Fact]
    public async Task Handle_SystemAdmin_ReturnsAllUsers()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        var users = new List<User>
        {
            UserBuilder.New().WithEmail("a@example.com").Build(),
            UserBuilder.New().WithEmail("b@example.com").Build(),
        };
        _userRepo.ListPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((users as IEnumerable<User>, 2));

        var result = await CreateHandler().Handle(new AdminListUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);

        var result = await CreateHandler().Handle(new AdminListUsersQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }

    [Fact]
    public async Task Handle_SystemAdmin_MapsSystemRoleToDto()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        var adminUser = UserBuilder.New()
            .WithEmail("admin@example.com")
            .WithSystemRole(SystemRole.SystemAdmin)
            .Build();
        _userRepo.ListPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<User> { adminUser } as IEnumerable<User>, 1));

        var result = await CreateHandler().Handle(new AdminListUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().SystemRole.Should().Be(SystemRole.SystemAdmin);
    }
}
