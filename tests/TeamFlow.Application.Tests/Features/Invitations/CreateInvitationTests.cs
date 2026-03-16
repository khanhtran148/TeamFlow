using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.Create;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Invitations;

[Collection("Auth")]
public sealed class CreateInvitationTests(PostgresCollectionFixture fixture)
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
    public async Task Handle_OrgOwnerOrAdmin_CanCreateInvitation(OrgRole role)
    {
        var org = await SeedOrgWithMemberAsync(role);
        var cmd = new CreateInvitationCommand(org.Id, "invite@example.com", OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Member);
        var cmd = new CreateInvitationCommand(org.Id, null, OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var cmd = new CreateInvitationCommand(org.Id, null, OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRawTokenInResponse()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(org.Id, "invite@example.com", OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.Token.Should().HaveLength(43); // base64url 32 bytes → 43 chars
    }

    [Fact]
    public async Task Handle_ValidCommand_StoresHashedToken()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(org.Id, null, OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var invitation = await DbContext.Set<Invitation>()
            .Where(i => i.OrganizationId == org.Id)
            .FirstOrDefaultAsync();
        invitation.Should().NotBeNull();
        invitation!.TokenHash.Should().NotBe(result.Value.Token);
        invitation.TokenHash.Length.Should().Be(64); // SHA-256 hex = 64 chars
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsSevenDayExpiry()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Owner);
        var before = DateTime.UtcNow.AddDays(6).AddHours(23);
        var cmd = new CreateInvitationCommand(org.Id, null, OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var invitation = await DbContext.Set<Invitation>()
            .Where(i => i.OrganizationId == org.Id)
            .FirstOrDefaultAsync();
        invitation!.ExpiresAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsPendingStatus()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(org.Id, null, OrgRole.Member);

        await Sender.Send(cmd);

        var invitation = await DbContext.Set<Invitation>()
            .Where(i => i.OrganizationId == org.Id)
            .FirstOrDefaultAsync();
        invitation!.Status.Should().Be(InviteStatus.Pending);
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        var cmd = new CreateInvitationCommand(Guid.NewGuid(), null, OrgRole.Member);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_CannotInviteAsOwner_ReturnsForbidden()
    {
        var org = await SeedOrgWithMemberAsync(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(org.Id, null, OrgRole.Owner);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_ReturnsValidationError()
    {
        var validator = new CreateInvitationValidator();
        var cmd = new CreateInvitationCommand(SeedOrgId, "not-an-email", OrgRole.Member);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInvitationCommand.Email));
    }

    [Fact]
    public async Task Validate_NullEmail_IsValid()
    {
        var validator = new CreateInvitationValidator();
        var cmd = new CreateInvitationCommand(SeedOrgId, null, OrgRole.Member);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(OrgRole.Member)]
    [InlineData(OrgRole.Admin)]
    public async Task Validate_ValidRole_IsValid(OrgRole role)
    {
        var validator = new CreateInvitationValidator();
        var cmd = new CreateInvitationCommand(SeedOrgId, null, role);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }
}
