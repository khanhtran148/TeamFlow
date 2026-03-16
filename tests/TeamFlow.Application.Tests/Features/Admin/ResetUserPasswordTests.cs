using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ResetUserPassword;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

public sealed class ResetUserPasswordTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AdminResetUserPasswordHandler CreateHandler() =>
        new(_userRepo, _refreshTokenRepo, _authService, _currentUser);

    private void SetupSystemAdmin()
    {
        _currentUser.SystemRole.Returns(SystemRole.SystemAdmin);
        _currentUser.Id.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_SystemAdmin_ResetsPasswordSuccessfully()
    {
        SetupSystemAdmin();
        var targetUser = UserBuilder.New().WithEmail("target@example.com").Build();
        _userRepo.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        _authService.HashPassword("NewPass@123").Returns("new-hashed-password");
        var cmd = new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SystemAdmin_HashesPassword()
    {
        SetupSystemAdmin();
        var targetUser = UserBuilder.New().Build();
        _userRepo.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        _authService.HashPassword("NewPass@123").Returns("new-hashed-password");
        var cmd = new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123");

        await CreateHandler().Handle(cmd, CancellationToken.None);

        targetUser.PasswordHash.Should().Be("new-hashed-password");
    }

    [Fact]
    public async Task Handle_SystemAdmin_SetsMustChangePasswordTrue()
    {
        SetupSystemAdmin();
        var targetUser = UserBuilder.New().WithMustChangePassword(false).Build();
        _userRepo.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed");
        var cmd = new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123");

        await CreateHandler().Handle(cmd, CancellationToken.None);

        targetUser.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SystemAdmin_RevokesAllRefreshTokens()
    {
        SetupSystemAdmin();
        var targetUser = UserBuilder.New().Build();
        _userRepo.GetByIdAsync(targetUser.Id, Arg.Any<CancellationToken>()).Returns(targetUser);
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed");
        var cmd = new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123");

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _refreshTokenRepo.Received(1).RevokeAllForUserAsync(targetUser.Id, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(SystemRole.User)]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden(SystemRole role)
    {
        _currentUser.SystemRole.Returns(role);
        var cmd = new AdminResetUserPasswordCommand(Guid.NewGuid(), "NewPass@123");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        SetupSystemAdmin();
        var userId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);
        var cmd = new AdminResetUserPasswordCommand(userId, "NewPass@123");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("short")]
    public async Task Validator_InvalidPassword_FailsValidation(string? password)
    {
        var validator = new AdminResetUserPasswordValidator();
        var cmd = new AdminResetUserPasswordCommand(Guid.NewGuid(), password!);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminResetUserPasswordValidator();
        var cmd = new AdminResetUserPasswordCommand(Guid.NewGuid(), "ValidPass@1");

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_EmptyUserId_FailsValidation()
    {
        var validator = new AdminResetUserPasswordValidator();
        var cmd = new AdminResetUserPasswordCommand(Guid.Empty, "ValidPass@1");

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}
