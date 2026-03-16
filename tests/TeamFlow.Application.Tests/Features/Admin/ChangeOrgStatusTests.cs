using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ChangeOrgStatus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ChangeOrgStatusTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly IInvitationRepository _invitationRepo = Substitute.For<IInvitationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminChangeOrgStatusHandler CreateHandler() =>
        new(_orgRepo, _invitationRepo, _currentUser);

    private void SetupSystemAdmin()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        _currentUser.Id.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_SystemAdmin_DeactivatesOrg()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithIsActive(true).Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var cmd = new AdminChangeOrgStatusCommand(org.Id, false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        org.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemAdmin_ActivatesOrg()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithIsActive(false).Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var cmd = new AdminChangeOrgStatusCommand(org.Id, true);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        org.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Deactivation_RevokesPendingInvitations()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithIsActive(true).Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var cmd = new AdminChangeOrgStatusCommand(org.Id, false);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _invitationRepo.Received(1).RevokePendingByOrgAsync(org.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Activation_DoesNotRevokePendingInvitations()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().WithIsActive(false).Build();
        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        var cmd = new AdminChangeOrgStatusCommand(org.Id, true);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _invitationRepo.DidNotReceive().RevokePendingByOrgAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var cmd = new AdminChangeOrgStatusCommand(Guid.NewGuid(), false);

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
        var cmd = new AdminChangeOrgStatusCommand(orgId, false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

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
