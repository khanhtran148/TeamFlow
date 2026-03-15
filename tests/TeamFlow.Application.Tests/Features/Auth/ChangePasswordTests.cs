using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.ChangePassword;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Auth;

public sealed class ChangePasswordTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    private static readonly Guid UserId = Guid.NewGuid();

    public ChangePasswordTests()
    {
        _currentUser.Id.Returns(UserId);
        _authService.HashPassword("NewPassword1").Returns("new-hashed-password");
    }

    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();

    private ChangePasswordHandler CreateHandler() => new(_userRepo, _refreshTokenRepo, _currentUser, _authService);

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ChangesSuccessfully()
    {
        var user = new User { Email = "user@test.com", Name = "Test", PasswordHash = "old-hash" };
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("OldPassword1", "old-hash").Returns(true);

        var cmd = new ChangePasswordCommand("OldPassword1", "NewPassword1");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hashed-password");
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsFailure()
    {
        var user = new User { Email = "user@test.com", Name = "Test", PasswordHash = "old-hash" };
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("wrong", "old-hash").Returns(false);

        var cmd = new ChangePasswordCommand("wrong", "NewPassword1");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("incorrect");
    }

    [Theory]
    [InlineData("", "NewPassword1")]
    [InlineData("OldPassword1", "")]
    [InlineData("OldPassword1", "short")]
    [InlineData("OldPassword1", "nouppercase1")]
    public async Task Handle_InvalidInput_FailsValidation(string currentPassword, string newPassword)
    {
        var validator = new ChangePasswordValidator();
        var cmd = new ChangePasswordCommand(currentPassword, newPassword);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
