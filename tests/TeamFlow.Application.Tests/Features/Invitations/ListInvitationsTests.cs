using FluentAssertions;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.List;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Invitations;

[Collection("Auth")]
public sealed class ListInvitationsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<Organization> SeedOrgWithMemberAsync(OrgRole role)
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        DbContext.Set<OrganizationMember>().Add(new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = SeedUserId,
            Role = role
        });
        await DbContext.SaveChangesAsync();

        return org;
    }

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OrgOwnerOrAdmin_ReturnsInvitations(OrgRole role)
    {
        var org = await SeedOrgWithMemberAsync(role);
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithStatus(InviteStatus.Pending)
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListInvitationsQuery(org.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Member);

        var result = await Sender.Send(new ListInvitationsQuery(org.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListInvitationsQuery(org.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_OrgOwner_ReturnsMappedDtos()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Owner);
        DbContext.Set<Invitation>().Add(InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithEmail("user@example.com")
            .WithRole(OrgRole.Admin)
            .WithStatus(InviteStatus.Pending)
            .Build());
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListInvitationsQuery(org.Id));

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Single();
        dto.Should().BeOfType<InvitationDto>();
        dto.Email.Should().Be("user@example.com");
        dto.Role.Should().Be(OrgRole.Admin);
        dto.Status.Should().Be(InviteStatus.Pending);
    }
}
