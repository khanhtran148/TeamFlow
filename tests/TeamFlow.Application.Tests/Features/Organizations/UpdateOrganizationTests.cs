using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.UpdateOrganization;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Organizations;

public sealed class UpdateOrganizationTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public UpdateOrganizationTests()
    {
        _currentUser.Id.Returns(_userId);
        _currentUser.SystemRole.Returns(SystemRole.User);

        var org = new Organization { Name = "Old Name", Slug = "old-name" };
        _orgRepo.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns(org);
        _orgRepo.ExistsBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _orgRepo.UpdateAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Organization>());
    }

    private UpdateOrganizationHandler CreateHandler() => new(_orgRepo, _memberRepo, _currentUser);

    [Fact]
    public async Task Handle_OrgOwner_CanUpdateNameAndSlug()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new UpdateOrganizationCommand(_orgId, "New Name", "new-name");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Slug.Should().Be("new-name");
    }

    [Fact]
    public async Task Handle_OrgAdmin_CanUpdateNameAndSlug()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Admin);
        var cmd = new UpdateOrganizationCommand(_orgId, "New Name", "new-name");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Member);
        var cmd = new UpdateOrganizationCommand(_orgId, "New Name", "new-name");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        // DomainError.Forbidden returns "Only Org Owner or Admin can update this organization."
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrgRole?)null);
        var cmd = new UpdateOrganizationCommand(_orgId, "New Name", "new-name");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        _orgRepo.GetByIdAsync(_orgId, Arg.Any<CancellationToken>()).Returns((Organization?)null);
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new UpdateOrganizationCommand(_orgId, "New Name", "new-name");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(),
            options => options.IgnoringCase());
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ReturnsConflict()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        _orgRepo.ExistsBySlugAsync("new-name", Arg.Any<CancellationToken>()).Returns(true);
        var cmd = new UpdateOrganizationCommand(_orgId, "New Name", "new-name");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("already exists", Exactly.Once(),
            options => options.IgnoringCase());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new UpdateOrganizationValidator(_orgRepo);
        var cmd = new UpdateOrganizationCommand(_orgId, name!, "valid-slug");

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOrganizationCommand.Name));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("this-slug-is-way-too-long-to-be-acceptable-for-any-org")]
    public async Task Validate_InvalidSlug_ReturnsValidationError(string slug)
    {
        _orgRepo.ExistsBySlugAsync(slug, Arg.Any<CancellationToken>()).Returns(false);
        var validator = new UpdateOrganizationValidator(_orgRepo);
        var cmd = new UpdateOrganizationCommand(_orgId, "Valid Name", slug);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
