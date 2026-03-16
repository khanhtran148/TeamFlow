using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.TransferOrgOwnership;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class TransferOrgOwnershipTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_SystemAdmin_TransfersOwnershipSuccessfully()
    {
        var org = OrganizationBuilder.New().Build();
        var currentOwner = UserBuilder.New().WithEmail("too-owner@example.com").Build();
        var newOwner = UserBuilder.New().WithEmail("too-newowner@example.com").Build();
        DbContext.Set<Organization>().Add(org);
        DbContext.Set<User>().AddRange(currentOwner, newOwner);
        await DbContext.SaveChangesAsync();

        DbContext.Set<OrganizationMember>().AddRange(
            new OrganizationMember { OrganizationId = org.Id, UserId = currentOwner.Id, Role = OrgRole.Owner },
            new OrganizationMember { OrganizationId = org.Id, UserId = newOwner.Id, Role = OrgRole.Admin }
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(org.Id, newOwner.Id));

        result.IsSuccess.Should().BeTrue();

        var newOwnerMembership = await DbContext.Set<OrganizationMember>()
            .Where(m => m.OrganizationId == org.Id && m.UserId == newOwner.Id)
            .SingleAsync();
        newOwnerMembership.Role.Should().Be(OrgRole.Owner);

        var oldOwnerMembership = await DbContext.Set<OrganizationMember>()
            .Where(m => m.OrganizationId == org.Id && m.UserId == currentOwner.Id)
            .SingleAsync();
        oldOwnerMembership.Role.Should().Be(OrgRole.Admin);
    }

    [Fact]
    public async Task Handle_NewOwnerNotMember_ReturnsBadRequest()
    {
        var org = OrganizationBuilder.New().Build();
        var newOwner = UserBuilder.New().WithEmail("too-notmember@example.com").Build();
        DbContext.Set<Organization>().Add(org);
        DbContext.Set<User>().Add(newOwner);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(org.Id, newOwner.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not a member");
    }

    [Fact]
    public async Task Handle_NewOwnerIsAlreadyOwner_ReturnsBadRequest()
    {
        var org = OrganizationBuilder.New().Build();
        var ownerUser = UserBuilder.New().WithEmail("too-alreadyowner@example.com").Build();
        DbContext.Set<Organization>().Add(org);
        DbContext.Set<User>().Add(ownerUser);
        await DbContext.SaveChangesAsync();

        DbContext.Set<OrganizationMember>().Add(
            new OrganizationMember { OrganizationId = org.Id, UserId = ownerUser.Id, Role = OrgRole.Owner });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(org.Id, ownerUser.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("already the owner");
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        var newOwner = UserBuilder.New().WithEmail("too-orgnotfound@example.com").Build();
        DbContext.Set<User>().Add(newOwner);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(Guid.NewGuid(), newOwner.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NewOwnerUserNotFound_ReturnsNotFound()
    {
        var org = OrganizationBuilder.New().Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(org.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task Validator_EmptyOrgId_FailsValidation()
    {
        var validator = new AdminTransferOrgOwnershipValidator();
        var cmd = new AdminTransferOrgOwnershipCommand(Guid.Empty, Guid.NewGuid());
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_EmptyNewOwnerId_FailsValidation()
    {
        var validator = new AdminTransferOrgOwnershipValidator();
        var cmd = new AdminTransferOrgOwnershipCommand(Guid.NewGuid(), Guid.Empty);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminTransferOrgOwnershipValidator();
        var cmd = new AdminTransferOrgOwnershipCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Auth")]
public sealed class TransferOrgOwnershipForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminTransferOrgOwnershipCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
