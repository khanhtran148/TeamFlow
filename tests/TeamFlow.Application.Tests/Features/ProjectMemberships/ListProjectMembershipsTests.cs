using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

public sealed class ListProjectMembershipsTests
{
    private readonly IProjectMembershipRepository _membershipRepo = Substitute.For<IProjectMembershipRepository>();

    private ListProjectMembershipsHandler CreateHandler() => new(_membershipRepo);

    [Fact]
    public async Task Handle_ProjectWithMembers_ReturnsMembershipDtos()
    {
        var projectId = Guid.NewGuid();
        var memberships = new List<ProjectMembership>
        {
            new() { ProjectId = projectId, MemberId = Guid.NewGuid(), MemberType = "User", Role = ProjectRole.Developer },
            new() { ProjectId = projectId, MemberId = Guid.NewGuid(), MemberType = "User", Role = ProjectRole.Viewer }
        };
        _membershipRepo.GetByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(memberships);

        var result = await CreateHandler().Handle(
            new ListProjectMembershipsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ProjectWithNoMembers_ReturnsEmptyList()
    {
        var projectId = Guid.NewGuid();
        _membershipRepo.GetByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<ProjectMembership>());

        var result = await CreateHandler().Handle(
            new ListProjectMembershipsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
