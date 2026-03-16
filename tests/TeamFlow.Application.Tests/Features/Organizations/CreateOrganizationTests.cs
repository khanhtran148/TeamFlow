using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.CreateOrganization;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Organizations;

public sealed class CreateOrganizationTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public CreateOrganizationTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        _orgRepo.AddAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Organization>());
        _orgRepo.ExistsBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    private CreateOrganizationHandler CreateHandler() => new(_orgRepo, _memberRepo, _currentUser);

    [Fact]
    public async Task Handle_SystemAdminWithValidCommand_ReturnsSuccessWithDto()
    {
        var cmd = new CreateOrganizationCommand("My Org", null);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Org");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SystemAdminWithValidCommand_GeneratesSlugFromName()
    {
        var cmd = new CreateOrganizationCommand("My Org", null);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("my-org");
    }

    [Fact]
    public async Task Handle_SystemAdminWithExplicitSlug_UsesProvidedSlug()
    {
        var cmd = new CreateOrganizationCommand("My Org", "custom-slug");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("custom-slug");
    }

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        _currentUser.SystemRole.Returns(SystemRole.User);
        var cmd = new CreateOrganizationCommand("My Org", null);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("SystemAdmin");
    }

    [Fact]
    public async Task Handle_SystemAdmin_CreatesOwnerMembershipForCreator()
    {
        var cmd = new CreateOrganizationCommand("My Org", null);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _memberRepo.Received(1).AddAsync(
            Arg.Is<OrganizationMember>(m =>
                m.UserId == _currentUser.Id &&
                m.Role == OrgRole.Owner),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SystemAdmin_PersistsOrganization()
    {
        var cmd = new CreateOrganizationCommand("My Org", null);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _orgRepo.Received(1).AddAsync(
            Arg.Is<Organization>(o => o.Name == "My Org"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new CreateOrganizationValidator(_orgRepo);
        var cmd = new CreateOrganizationCommand(name!, null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrganizationCommand.Name));
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        var validator = new CreateOrganizationValidator(_orgRepo);
        var cmd = new CreateOrganizationCommand(new string('A', 101), null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_DuplicateSlug_ReturnsValidationError()
    {
        _orgRepo.ExistsBySlugAsync("existing-slug", Arg.Any<CancellationToken>()).Returns(true);
        var validator = new CreateOrganizationValidator(_orgRepo);
        var cmd = new CreateOrganizationCommand("My Org", "existing-slug");

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrganizationCommand.Slug));
    }
}
