using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Invitations;
using TeamFlow.Application.Features.Invitations.Create;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Invitations;

public sealed class CreateInvitationTests
{
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public CreateInvitationTests()
    {
        _currentUser.Id.Returns(_userId);
        _currentUser.Email.Returns("admin@teamflow.dev");

        _invitationRepo.AddAsync(Arg.Any<Invitation>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Invitation>());

        _orgRepo.GetByIdAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns(new Organization { Name = "Test Org", Slug = "test-org" });
    }

    private CreateInvitationHandler CreateHandler() =>
        new(_invitationRepo, _memberRepo, _orgRepo, _currentUser);

    [Theory]
    [InlineData(OrgRole.Owner)]
    [InlineData(OrgRole.Admin)]
    public async Task Handle_OrgOwnerOrAdmin_CanCreateInvitation(OrgRole role)
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(role);
        var cmd = new CreateInvitationCommand(_orgId, "invite@example.com", OrgRole.Member);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrgMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Member);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns((OrgRole?)null);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Owner or Admin");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRawTokenInResponse()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(_orgId, "invite@example.com", OrgRole.Member);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.Token.Should().HaveLength(43); // base64url 32 bytes → 43 chars
    }

    [Fact]
    public async Task Handle_ValidCommand_StoresHashedToken()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _invitationRepo.Received(1).AddAsync(
            Arg.Is<Invitation>(i =>
                i.TokenHash != result.Value.Token && // hash != raw token
                i.TokenHash.Length == 64),            // SHA-256 hex = 64 chars
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsSevenDayExpiry()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);
        var before = DateTime.UtcNow.AddDays(6).AddHours(23);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _invitationRepo.Received(1).AddAsync(
            Arg.Is<Invitation>(i => i.ExpiresAt > before),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsPendingStatus()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _invitationRepo.Received(1).AddAsync(
            Arg.Is<Invitation>(i => i.Status == InviteStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        _orgRepo.GetByIdAsync(_orgId, Arg.Any<CancellationToken>())
            .Returns((Organization?)null);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", Exactly.Once(), o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_CannotInviteAsOwner_ReturnsForbidden()
    {
        _memberRepo.GetMemberRoleAsync(_orgId, _userId, Arg.Any<CancellationToken>())
            .Returns(OrgRole.Owner);
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Owner);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("Owner", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Validate_InvalidEmailFormat_ReturnsValidationError()
    {
        var validator = new CreateInvitationValidator();
        var cmd = new CreateInvitationCommand(_orgId, "not-an-email", OrgRole.Member);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateInvitationCommand.Email));
    }

    [Fact]
    public async Task Validate_NullEmail_IsValid()
    {
        var validator = new CreateInvitationValidator();
        var cmd = new CreateInvitationCommand(_orgId, null, OrgRole.Member);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(OrgRole.Member)]
    [InlineData(OrgRole.Admin)]
    public async Task Validate_ValidRole_IsValid(OrgRole role)
    {
        var validator = new CreateInvitationValidator();
        var cmd = new CreateInvitationCommand(_orgId, null, role);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeTrue();
    }
}
