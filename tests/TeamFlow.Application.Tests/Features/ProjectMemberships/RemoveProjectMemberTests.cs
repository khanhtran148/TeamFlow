using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.RemoveProjectMember;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

public sealed class RemoveProjectMemberTests
{
    private readonly IProjectMembershipRepository _membershipRepo = Substitute.For<IProjectMembershipRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public RemoveProjectMemberTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private RemoveProjectMemberHandler CreateHandler() =>
        new(_membershipRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ExistingMembership_DeletesSuccessfully()
    {
        var membership = new ProjectMembership
        {
            ProjectId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Role = ProjectRole.Developer
        };
        _membershipRepo.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(membership.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _membershipRepo.Received(1).DeleteAsync(membership, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MembershipNotFound_ReturnsFailure()
    {
        _membershipRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProjectMembership?)null);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var membership = new ProjectMembership
        {
            ProjectId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Role = ProjectRole.Developer
        };
        _membershipRepo.GetByIdAsync(membership.Id, Arg.Any<CancellationToken>()).Returns(membership);
        _permissions.HasPermissionAsync(ActorId, membership.ProjectId, Permission.Project_ManageMembers, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(membership.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
