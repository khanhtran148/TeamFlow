using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ChangeOrgStatus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ChangeOrgStatusTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_SystemAdmin_DeactivatesOrg()
    {
        var org = OrganizationBuilder.New().WithIsActive(true).Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminChangeOrgStatusCommand(org.Id, false));

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.Set<Organization>().FindAsync(org.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemAdmin_ActivatesOrg()
    {
        var org = OrganizationBuilder.New().WithIsActive(false).Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminChangeOrgStatusCommand(org.Id, true));

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.Set<Organization>().FindAsync(org.Id);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Deactivation_RevokesPendingInvitations()
    {
        var org = OrganizationBuilder.New().WithIsActive(true).Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var pendingInv = InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithStatus(InviteStatus.Pending)
            .Build();
        DbContext.Set<Invitation>().Add(pendingInv);
        await DbContext.SaveChangesAsync();

        await Sender.Send(new AdminChangeOrgStatusCommand(org.Id, false));

        var inv = await DbContext.Set<Invitation>().FindAsync(pendingInv.Id);
        inv!.Status.Should().Be(InviteStatus.Revoked);
    }

    [Fact]
    public async Task Handle_Activation_DoesNotRevokePendingInvitations()
    {
        var org = OrganizationBuilder.New().WithIsActive(false).Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var pendingInv = InvitationBuilder.New()
            .WithOrganization(org.Id)
            .WithInvitedBy(SeedUserId)
            .WithStatus(InviteStatus.Pending)
            .Build();
        DbContext.Set<Invitation>().Add(pendingInv);
        await DbContext.SaveChangesAsync();

        await Sender.Send(new AdminChangeOrgStatusCommand(org.Id, true));

        var inv = await DbContext.Set<Invitation>().FindAsync(pendingInv.Id);
        inv!.Status.Should().Be(InviteStatus.Pending);
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        var result = await Sender.Send(new AdminChangeOrgStatusCommand(Guid.NewGuid(), false));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task Validator_EmptyOrgId_FailsValidation()
    {
        var validator = new AdminChangeOrgStatusValidator();
        var cmd = new AdminChangeOrgStatusCommand(Guid.Empty, false);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminChangeOrgStatusValidator();
        var cmd = new AdminChangeOrgStatusCommand(Guid.NewGuid(), true);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Auth")]
public sealed class ChangeOrgStatusForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminChangeOrgStatusCommand(Guid.NewGuid(), false));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
