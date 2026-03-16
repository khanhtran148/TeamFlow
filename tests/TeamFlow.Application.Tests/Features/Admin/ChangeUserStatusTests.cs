using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ChangeUserStatus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ChangeUserStatusTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly Guid _adminId = Guid.NewGuid();

    private AdminChangeUserStatusHandler CreateHandler() =>
        new(_userRepo, _refreshTokenRepo, _currentUser);

    private void SetupSystemAdmin()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        _currentUser.Id.Returns(_adminId);
    }

    [Fact]
    public async Task Handle_SystemAdmin_DeactivatesUser()
    {
        SetupSystemAdmin();
        var target = UserBuilder.New().WithIsActive(true).Build();
        _userRepo.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        var cmd = new AdminChangeUserStatusCommand(target.Id, false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemAdmin_ActivatesUser()
    {
        SetupSystemAdmin();
        var target = UserBuilder.New().WithIsActive(false).Build();
        _userRepo.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        var cmd = new AdminChangeUserStatusCommand(target.Id, true);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Deactivation_RevokesAllRefreshTokens()
    {
        SetupSystemAdmin();
        var target = UserBuilder.New().WithIsActive(true).Build();
        _userRepo.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        var cmd = new AdminChangeUserStatusCommand(target.Id, false);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _refreshTokenRepo.Received(1).RevokeAllForUserAsync(target.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Activation_DoesNotRevokeRefreshTokens()
    {
        SetupSystemAdmin();
        var target = UserBuilder.New().WithIsActive(false).Build();
        _userRepo.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        var cmd = new AdminChangeUserStatusCommand(target.Id, true);

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _refreshTokenRepo.DidNotReceive().RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var cmd = new AdminChangeUserStatusCommand(Guid.NewGuid(), false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }

    [Fact]
    public async Task Handle_DeactivateSelf_ReturnsForbidden()
    {
        SetupSystemAdmin();
        var cmd = new AdminChangeUserStatusCommand(_adminId, false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("own account");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        SetupSystemAdmin();
        var userId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);
        var cmd = new AdminChangeUserStatusCommand(userId, false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DeactivateLastSystemAdmin_ReturnsForbidden()
    {
        SetupSystemAdmin();
        var lastAdmin = UserBuilder.New()
            .WithSystemRole(SystemRole.SystemAdmin)
            .WithIsActive(true)
            .Build();
        _userRepo.GetByIdAsync(lastAdmin.Id, Arg.Any<CancellationToken>()).Returns(lastAdmin);
        // Only one admin in the list
        _userRepo.ListAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { lastAdmin });
        var cmd = new AdminChangeUserStatusCommand(lastAdmin.Id, false);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("last system administrator");
    }

    [Fact]
    public async Task Validator_EmptyUserId_FailsValidation()
    {
        var validator = new AdminChangeUserStatusValidator();
        var cmd = new AdminChangeUserStatusCommand(Guid.Empty, false);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminChangeUserStatusValidator();
        var cmd = new AdminChangeUserStatusCommand(Guid.NewGuid(), true);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeTrue();
    }
}
