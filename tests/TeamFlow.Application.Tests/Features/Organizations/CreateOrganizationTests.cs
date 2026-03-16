using FluentAssertions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.CreateOrganization;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace TeamFlow.Application.Tests.Features.Organizations;

/// <summary>Current user stub with SystemAdmin role for org creation tests.</summary>
file sealed class SystemAdminCurrentUser : ICurrentUser
{
    public Guid Id => PostgresCollectionFixture.SeedUserId;
    public string Email => "test@teamflow.dev";
    public string Name => "Test User";
    public bool IsAuthenticated => true;
    public SystemRole SystemRole => SystemRole.SystemAdmin;
}

[Collection("Projects")]
public sealed class CreateOrganizationTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, SystemAdminCurrentUser>();
    }

    [Fact]
    public async Task Handle_SystemAdminWithValidCommand_ReturnsSuccessWithDto()
    {
        var cmd = new CreateOrganizationCommand("My New Org " + Guid.NewGuid().ToString("N")[..6], null);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().StartWith("My New Org");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SystemAdminWithValidCommand_GeneratesSlugFromName()
    {
        var cmd = new CreateOrganizationCommand("My Org Slug Test", null);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("my-org-slug-test");
    }

    [Fact]
    public async Task Handle_SystemAdminWithExplicitSlug_UsesProvidedSlug()
    {
        var slug = "custom-slug-" + Guid.NewGuid().ToString("N")[..6];
        var cmd = new CreateOrganizationCommand("My Org With Slug", slug);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task Handle_SystemAdmin_CreatesOwnerMembershipForCreator()
    {
        var cmd = new CreateOrganizationCommand("Org With Owner " + Guid.NewGuid().ToString("N")[..6], null);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var orgId = result.Value.Id;
        var membership = DbContext.OrganizationMembers
            .FirstOrDefault(m => m.OrganizationId == orgId && m.UserId == SeedUserId);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(OrgRole.Owner);
    }

    [Fact]
    public async Task Handle_SystemAdmin_PersistsOrganization()
    {
        var name = "Persisted Org " + Guid.NewGuid().ToString("N")[..6];
        var cmd = new CreateOrganizationCommand(name, null);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var org = DbContext.Organizations.FirstOrDefault(o => o.Name == name);
        org.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var orgRepo = DbContext.Set<TeamFlow.Domain.Entities.Organization>();
        var validator = new CreateOrganizationValidator(
            new TeamFlow.Infrastructure.Repositories.OrganizationRepository(DbContext));
        var cmd = new CreateOrganizationCommand(name!, null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrganizationCommand.Name));
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        var validator = new CreateOrganizationValidator(
            new TeamFlow.Infrastructure.Repositories.OrganizationRepository(DbContext));
        var cmd = new CreateOrganizationCommand(new string('A', 101), null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_DuplicateSlug_ReturnsValidationError()
    {
        var existingOrg = OrganizationBuilder.New().WithSlug("existing-test-slug").Build();
        DbContext.Organizations.Add(existingOrg);
        await DbContext.SaveChangesAsync();

        var validator = new CreateOrganizationValidator(
            new TeamFlow.Infrastructure.Repositories.OrganizationRepository(DbContext));
        var cmd = new CreateOrganizationCommand("My Org", "existing-test-slug");

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrganizationCommand.Slug));
    }
}

[Collection("Projects")]
public sealed class CreateOrganizationForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        // Default TestCurrentUser has SystemRole.User
        var cmd = new CreateOrganizationCommand("My Org", null);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SystemAdmin");
    }
}
