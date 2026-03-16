using FluentAssertions;
using TeamFlow.Application.Features.Organizations.UpdateOrganization;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Repositories;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Organizations;

[Collection("Projects")]
public sealed class UpdateOrganizationTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<(TeamFlow.Domain.Entities.Organization Org, Guid OrgId)> SeedOrgWithMemberAsync(OrgRole role)
    {
        var org = OrganizationBuilder.New()
            .WithName("Old Name")
            .WithSlug("update-test-" + Guid.NewGuid().ToString("N")[..8])
            .Build();
        DbContext.Organizations.Add(org);

        var member = OrganizationMemberBuilder.New()
            .WithOrganization(org.Id)
            .WithUser(SeedUserId)
            .WithRole(role)
            .Build();
        DbContext.OrganizationMembers.Add(member);
        await DbContext.SaveChangesAsync();
        return (org, org.Id);
    }

    [Fact]
    public async Task Handle_OrgOwner_CanUpdateNameAndSlug()
    {
        var (_, orgId) = await SeedOrgWithMemberAsync(OrgRole.Owner);
        var newSlug = "new-name-" + Guid.NewGuid().ToString("N")[..6];
        var cmd = new UpdateOrganizationCommand(orgId, "New Name", newSlug);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Slug.Should().Be(newSlug);
    }

    [Fact]
    public async Task Handle_OrgAdmin_CanUpdateNameAndSlug()
    {
        var (_, orgId) = await SeedOrgWithMemberAsync(OrgRole.Admin);
        var newSlug = "admin-slug-" + Guid.NewGuid().ToString("N")[..6];
        var cmd = new UpdateOrganizationCommand(orgId, "New Name", newSlug);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        var (_, orgId) = await SeedOrgWithMemberAsync(OrgRole.Member);
        var cmd = new UpdateOrganizationCommand(orgId, "New Name", "new-name");

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var org = OrganizationBuilder.New()
            .WithSlug("no-member-" + Guid.NewGuid().ToString("N")[..8])
            .Build();
        DbContext.Organizations.Add(org);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateOrganizationCommand(org.Id, "New Name", "new-name");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        var cmd = new UpdateOrganizationCommand(Guid.NewGuid(), "New Name", "new-name");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ReturnsConflict()
    {
        var existingOrg = OrganizationBuilder.New().WithSlug("taken-slug-" + Guid.NewGuid().ToString("N")[..6]).Build();
        DbContext.Organizations.Add(existingOrg);
        var (_, orgId) = await SeedOrgWithMemberAsync(OrgRole.Owner);

        var cmd = new UpdateOrganizationCommand(orgId, "New Name", existingOrg.Slug);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already exists", o => o.IgnoringCase());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new UpdateOrganizationValidator(new OrganizationRepository(DbContext));
        var cmd = new UpdateOrganizationCommand(SeedOrgId, name!, "valid-slug");

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOrganizationCommand.Name));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("this-slug-is-way-too-long-to-be-acceptable-for-any-org")]
    public async Task Validate_InvalidSlug_ReturnsValidationError(string slug)
    {
        var validator = new UpdateOrganizationValidator(new OrganizationRepository(DbContext));
        var cmd = new UpdateOrganizationCommand(SeedOrgId, "Valid Name", slug);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
