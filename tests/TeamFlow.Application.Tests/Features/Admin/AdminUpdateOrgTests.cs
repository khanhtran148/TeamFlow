using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.UpdateOrganization;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class AdminUpdateOrgTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_SystemAdmin_UpdatesNameAndSlug()
    {
        var org = OrganizationBuilder.New().WithName("Old Name").WithSlug("old-slug-auo1").Build();
        DbContext.Set<TeamFlow.Domain.Entities.Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var cmd = new AdminUpdateOrgCommand(org.Id, "New Name", "new-slug-auo1");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.Set<TeamFlow.Domain.Entities.Organization>().FindAsync(org.Id);
        updated!.Name.Should().Be("New Name");
        updated.Slug.Should().Be("new-slug-auo1");
    }

    [Fact]
    public async Task Handle_SameSlug_DoesNotConflict()
    {
        var org = OrganizationBuilder.New().WithName("Name").WithSlug("same-slug-auo2").Build();
        DbContext.Set<TeamFlow.Domain.Entities.Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        var cmd = new AdminUpdateOrgCommand(org.Id, "Updated Name", "same-slug-auo2");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateSlugOnAnotherOrg_ReturnsConflict()
    {
        var org1 = OrganizationBuilder.New().WithName("Org1").WithSlug("taken-slug-auo3").Build();
        var org2 = OrganizationBuilder.New().WithName("Org2").WithSlug("original-slug-auo3").Build();
        DbContext.Set<TeamFlow.Domain.Entities.Organization>().AddRange(org1, org2);
        await DbContext.SaveChangesAsync();

        var cmd = new AdminUpdateOrgCommand(org2.Id, "Org2", "taken-slug-auo3");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("already in use");
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        var cmd = new AdminUpdateOrgCommand(Guid.NewGuid(), "Name", "slug-auo4");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Theory]
    [InlineData("", "valid-slug")]
    [InlineData(null, "valid-slug")]
    public async Task Validator_EmptyName_FailsValidation(string? name, string slug)
    {
        var validator = new AdminUpdateOrgValidator();
        var cmd = new AdminUpdateOrgCommand(Guid.NewGuid(), name!, slug);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Valid Name", "")]
    [InlineData("Valid Name", null)]
    [InlineData("Valid Name", "INVALID SLUG")]
    [InlineData("Valid Name", "invalid_slug")]
    public async Task Validator_InvalidSlug_FailsValidation(string name, string? slug)
    {
        var validator = new AdminUpdateOrgValidator();
        var cmd = new AdminUpdateOrgCommand(Guid.NewGuid(), name, slug!);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminUpdateOrgValidator();
        var cmd = new AdminUpdateOrgCommand(Guid.NewGuid(), "Valid Org Name", "valid-slug-123");
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Auth")]
public sealed class AdminUpdateOrgForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        // Default TestCurrentUser has SystemRole.User
        var cmd = new AdminUpdateOrgCommand(Guid.NewGuid(), "Name", "slug");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
