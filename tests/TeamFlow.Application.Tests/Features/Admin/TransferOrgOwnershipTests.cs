using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.TransferOrgOwnership;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class TransferOrgOwnershipTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly IOrganizationMemberRepository _memberRepo = Substitute.For<IOrganizationMemberRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminTransferOrgOwnershipHandler CreateHandler() =>
        new(_orgRepo, _memberRepo, _userRepo, _currentUser);

    private void SetupSystemAdmin() =>
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);

    [Fact]
    public async Task Handle_SystemAdmin_TransfersOwnershipSuccessfully()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().Build();
        var currentOwnerUser = UserBuilder.New().Build();
        var newOwnerUser = UserBuilder.New().Build();

        var ownerMembership = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = currentOwnerUser.Id,
            Role = OrgRole.Owner
        };
        var newOwnerMembership = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = newOwnerUser.Id,
            Role = OrgRole.Admin
        };

        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _userRepo.GetByIdAsync(newOwnerUser.Id, Arg.Any<CancellationToken>()).Returns(newOwnerUser);
        _memberRepo.GetByOrgAndUserAsync(org.Id, newOwnerUser.Id, Arg.Any<CancellationToken>())
            .Returns(newOwnerMembership);
        _memberRepo.GetByOrgAndUserAsync(org.Id, Arg.Is<Guid>(id => id != newOwnerUser.Id), Arg.Any<CancellationToken>())
            .Returns(ownerMembership);

        // Simulate finding current owner via listing
        _memberRepo.ListByOrgWithUsersAsync(org.Id, Arg.Any<CancellationToken>())
            .Returns(new List<(OrganizationMember, User)>
            {
                (ownerMembership, currentOwnerUser),
                (newOwnerMembership, newOwnerUser)
            });

        var cmd = new AdminTransferOrgOwnershipCommand(org.Id, newOwnerUser.Id);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        newOwnerMembership.Role.Should().Be(OrgRole.Owner);
        ownerMembership.Role.Should().Be(OrgRole.Admin);
    }

    [Fact]
    public async Task Handle_NewOwnerNotMember_ReturnsBadRequest()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().Build();
        var newOwnerUser = UserBuilder.New().Build();

        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _userRepo.GetByIdAsync(newOwnerUser.Id, Arg.Any<CancellationToken>()).Returns(newOwnerUser);
        _memberRepo.GetByOrgAndUserAsync(org.Id, newOwnerUser.Id, Arg.Any<CancellationToken>())
            .Returns((OrganizationMember?)null);

        var cmd = new AdminTransferOrgOwnershipCommand(org.Id, newOwnerUser.Id);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not a member");
    }

    [Fact]
    public async Task Handle_NewOwnerIsAlreadyOwner_ReturnsBadRequest()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().Build();
        var ownerUser = UserBuilder.New().Build();

        var ownerMembership = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = ownerUser.Id,
            Role = OrgRole.Owner
        };

        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _userRepo.GetByIdAsync(ownerUser.Id, Arg.Any<CancellationToken>()).Returns(ownerUser);
        _memberRepo.GetByOrgAndUserAsync(org.Id, ownerUser.Id, Arg.Any<CancellationToken>())
            .Returns(ownerMembership);

        var cmd = new AdminTransferOrgOwnershipCommand(org.Id, ownerUser.Id);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("already the owner");
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var cmd = new AdminTransferOrgOwnershipCommand(Guid.NewGuid(), Guid.NewGuid());

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
        var cmd = new AdminTransferOrgOwnershipCommand(orgId, Guid.NewGuid());

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NewOwnerUserNotFound_ReturnsNotFound()
    {
        SetupSystemAdmin();
        var org = OrganizationBuilder.New().Build();
        var newOwnerUserId = Guid.NewGuid();

        _orgRepo.GetByIdAsync(org.Id, Arg.Any<CancellationToken>()).Returns(org);
        _userRepo.GetByIdAsync(newOwnerUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var cmd = new AdminTransferOrgOwnershipCommand(org.Id, newOwnerUserId);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

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
