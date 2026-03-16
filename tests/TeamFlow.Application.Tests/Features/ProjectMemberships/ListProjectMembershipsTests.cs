using FluentAssertions;
using TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

[Collection("Projects")]
public sealed class ListProjectMembershipsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ProjectWithMembers_ReturnsMembershipDtosWithNames()
    {
        var project = await SeedProjectAsync();

        var user1 = UserBuilder.New().WithName("Alice Johnson").Build();
        var user2 = UserBuilder.New().WithName("Bob Smith").Build();
        DbContext.Users.AddRange(user1, user2);
        await DbContext.SaveChangesAsync();

        var m1 = ProjectMembershipBuilder.New()
            .WithProject(project.Id)
            .WithMember(user1.Id)
            .WithMemberType("User")
            .WithRole(ProjectRole.Developer)
            .Build();
        var m2 = ProjectMembershipBuilder.New()
            .WithProject(project.Id)
            .WithMember(user2.Id)
            .WithMemberType("User")
            .AsViewer()
            .Build();
        DbContext.ProjectMemberships.AddRange(m1, m2);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListProjectMembershipsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(d => d.MemberName == "Alice Johnson");
        result.Value.Should().Contain(d => d.MemberName == "Bob Smith");
    }

    [Fact]
    public async Task Handle_ProjectWithNoMembers_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListProjectMembershipsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MemberNotInUserTable_ShowsUnknown()
    {
        var project = await SeedProjectAsync();
        var unknownUserId = Guid.NewGuid();

        var membership = ProjectMembershipBuilder.New()
            .WithProject(project.Id)
            .WithMember(unknownUserId)
            .WithMemberType("User")
            .WithRole(ProjectRole.Developer)
            .Build();
        DbContext.ProjectMemberships.Add(membership);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListProjectMembershipsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.First().MemberName.Should().Be("Unknown");
    }
}
