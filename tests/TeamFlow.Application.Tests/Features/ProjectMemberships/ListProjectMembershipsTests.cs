using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

public sealed class ListProjectMembershipsTests
{
    private readonly IProjectMembershipRepository _membershipRepo = Substitute.For<IProjectMembershipRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();

    private ListProjectMembershipsHandler CreateHandler() => new(_membershipRepo, _userRepo);

    [Fact]
    public async Task Handle_ProjectWithMembers_ReturnsMembershipDtosWithNames()
    {
        var projectId = Guid.NewGuid();
        var user1 = UserBuilder.New().WithName("Alice Johnson").Build();
        var user2 = UserBuilder.New().WithName("Bob Smith").Build();
        var memberships = new List<ProjectMembership>
        {
            new() { ProjectId = projectId, MemberId = user1.Id, MemberType = "User", Role = ProjectRole.Developer },
            new() { ProjectId = projectId, MemberId = user2.Id, MemberType = "User", Role = ProjectRole.Viewer }
        };
        _membershipRepo.GetByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(memberships);

        _userRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user1, user2 });

        var result = await CreateHandler().Handle(
            new ListProjectMembershipsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(d => d.MemberName == "Alice Johnson");
        result.Value.Should().Contain(d => d.MemberName == "Bob Smith");
    }

    [Fact]
    public async Task Handle_ProjectWithNoMembers_ReturnsEmptyList()
    {
        var projectId = Guid.NewGuid();
        _membershipRepo.GetByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProjectMembership>());
        _userRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        var result = await CreateHandler().Handle(
            new ListProjectMembershipsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MemberNotInUserTable_ShowsUnknown()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var memberships = new List<ProjectMembership>
        {
            new() { ProjectId = projectId, MemberId = userId, MemberType = "User", Role = ProjectRole.Developer }
        };
        _membershipRepo.GetByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(memberships);
        _userRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());

        var result = await CreateHandler().Handle(
            new ListProjectMembershipsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.First().MemberName.Should().Be("Unknown");
    }
}
