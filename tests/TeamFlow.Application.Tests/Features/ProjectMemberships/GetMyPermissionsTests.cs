using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.GetMyPermissions;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

public sealed class GetMyPermissionsTests
{
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public GetMyPermissionsTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private GetMyPermissionsHandler CreateHandler() => new(_permissions, _currentUser);

    [Fact]
    public async Task Handle_UserWithRole_ReturnsRoleAndPermissions()
    {
        var projectId = Guid.NewGuid();
        _permissions.GetEffectiveRoleAsync(ActorId, projectId, Arg.Any<CancellationToken>())
            .Returns(ProjectRole.Developer);

        var result = await CreateHandler().Handle(new GetMyPermissionsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Developer");
        result.Value.Permissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithNoRole_ReturnsEmptyPermissions()
    {
        var projectId = Guid.NewGuid();
        _permissions.GetEffectiveRoleAsync(ActorId, projectId, Arg.Any<CancellationToken>())
            .Returns((ProjectRole?)null);

        var result = await CreateHandler().Handle(new GetMyPermissionsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().BeNull();
        result.Value.Permissions.Should().BeEmpty();
    }
}
