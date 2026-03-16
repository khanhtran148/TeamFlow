using FluentAssertions;
using TeamFlow.Application.Features.OrgMembers.List;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.OrgMembers;

[Collection("Projects")]
public sealed class ListMembersTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_OrgMember_ReturnsMembers()
    {
        var member = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Owner)
            .Build();
        DbContext.OrganizationMembers.Add(member);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListOrgMembersQuery(SeedOrgId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(d => d.UserId == SeedUserId);
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        // SeedUserId has no membership in SeedOrgId
        var result = await Sender.Send(new ListOrgMembersQuery(SeedOrgId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("member", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_OrgMember_MapsDtoCorrectly()
    {
        var member = OrganizationMemberBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithUser(SeedUserId)
            .WithRole(OrgRole.Admin)
            .Build();
        DbContext.OrganizationMembers.Add(member);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListOrgMembersQuery(SeedOrgId));

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First(d => d.UserId == SeedUserId);
        dto.UserId.Should().Be(SeedUserId);
        dto.UserName.Should().Be("Test User");
        dto.UserEmail.Should().Be("test@teamflow.dev");
        dto.Role.Should().Be(OrgRole.Admin);
    }
}
