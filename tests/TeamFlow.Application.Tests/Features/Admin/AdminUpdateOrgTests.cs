using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.UpdateOrganization;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class AdminUpdateOrgTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminUpdateOrgHandler CreateHandler() => new(_orgRepo, _currentUser);

    private void SetupSystemAdmin() =>
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);

    [Fact]
    public async Task Handle_SystemAdmin_UpdatesNameAndSlug()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithName("Old Name").WithSlug("old-slug").Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _orgRepo.ExistsBySlugAsync("new-slug", Arg.Any<CancellationToken>()).Returns(false);
        var cmd = new AdminUpdateOrgCommand(org.Id, "New Name", "new-slug");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        org.Name.Should().Be("New Name");
        org.Slug.Should().Be("new-slug");
    }

    [Fact]
    public async Task Handle_SameSlug_DoesNotConflict()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithName("Name").WithSlug("same-slug").Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        // ExistsBySlug would return true for "same-slug" but we exclude the current org
        var cmd = new AdminUpdateOrgCommand(org.Id, "Updated Name", "same-slug");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateSlugOnAnotherOrg_ReturnsConflict()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithName("Name").WithSlug("old-slug").Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _orgRepo.ExistsBySlugAsync("taken-slug", Arg.Any<CancellationToken>()).Returns(true);
        var cmd = new AdminUpdateOrgCommand(org.Id, "Name", "taken-slug");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("already in use");
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var cmd = new AdminUpdateOrgCommand(Guid.NewGuid(), "Name", "slug");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        SetupSystemAdmin();
        var orgId = Guid.NewGuid();
        _orgRepo.GetByIdAsync(orgId, Arg.Any<CancellationToken>()).Returns((Organization?)null);
        var cmd = new AdminUpdateOrgCommand(orgId, "Name", "slug");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

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
