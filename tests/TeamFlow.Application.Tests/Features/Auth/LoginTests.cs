using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.Login;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Auth;

public sealed class LoginTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    private static readonly User TestUser = new()
    {
        Email = "user@test.com",
        Name = "Test User",
        PasswordHash = "hashed-password"
    };

    public LoginTests()
    {
        _authService.GenerateJwt(Arg.Any<User>()).Returns("jwt-token");
        _authService.GenerateRefreshToken().Returns("refresh-token");
        _authService.HashToken(Arg.Any<string>()).Returns("hashed-refresh-token");
    }

    private LoginHandler CreateHandler() => new(_userRepo, _refreshTokenRepo, _authService);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        _userRepo.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(TestUser);
        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);
        var cmd = new LoginCommand("user@test.com", "Password1");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(TestUser);
        _authService.VerifyPassword("wrong", "hashed-password").Returns(false);
        var cmd = new LoginCommand("user@test.com", "wrong");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync("nobody@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        var cmd = new LoginCommand("nobody@test.com", "Password1");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_UserWithMustChangePassword_ReturnsFlag()
    {
        var user = new User
        {
            Email = "admin@test.com",
            Name = "Admin",
            PasswordHash = "hashed-password",
            MustChangePassword = true
        };
        _userRepo.GetByEmailAsync("admin@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);
        var cmd = new LoginCommand("admin@test.com", "Password1");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserWithoutMustChangePassword_ReturnsFlagFalse()
    {
        _userRepo.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(TestUser);
        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);
        var cmd = new LoginCommand("user@test.com", "Password1");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsForbidden()
    {
        var deactivatedUser = new User
        {
            Email = "deactivated@test.com",
            Name = "Deactivated User",
            PasswordHash = "hashed-password",
            IsActive = false
        };
        _userRepo.GetByEmailAsync("deactivated@test.com", Arg.Any<CancellationToken>()).Returns(deactivatedUser);
        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);
        var cmd = new LoginCommand("deactivated@test.com", "Password1");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("deactivated");
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("user@test.com", "")]
    public async Task Handle_EmptyFields_FailsValidation(string email, string password)
    {
        var validator = new LoginValidator();
        var cmd = new LoginCommand(email, password);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
