using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.Register;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Auth;

public sealed class RegisterTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    public RegisterTests()
    {
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed-password");
        _authService.GenerateJwt(Arg.Any<User>()).Returns("jwt-token");
        _authService.GenerateRefreshToken().Returns("refresh-token");
        _authService.HashToken(Arg.Any<string>()).Returns("hashed-refresh-token");
        _userRepo.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<User>());
    }

    private RegisterHandler CreateHandler() => new(_userRepo, _refreshTokenRepo, _authService);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTokens()
    {
        var cmd = new RegisterCommand("user@test.com", "Password1", "Test User");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsUser()
    {
        var cmd = new RegisterCommand("user@test.com", "Password1", "Test User");

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "user@test.com" && u.Name == "Test User"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesEmailToLowerCase()
    {
        var cmd = new RegisterCommand("USER@TEST.COM", "Password1", "Test User");

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "user@test.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictError()
    {
        _userRepo.ExistsByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(true);
        var cmd = new RegisterCommand("user@test.com", "Password1", "Test User");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Theory]
    [InlineData("", "Password1", "Name")]
    [InlineData("not-an-email", "Password1", "Name")]
    [InlineData("user@test.com", "", "Name")]
    [InlineData("user@test.com", "short", "Name")]
    [InlineData("user@test.com", "nouppercase1", "Name")]
    [InlineData("user@test.com", "NOLOWERCASE1", "Name")]
    [InlineData("user@test.com", "NoDigits!", "Name")]
    [InlineData("user@test.com", "Password1", "")]
    public async Task Handle_InvalidInput_FailsValidation(string email, string password, string name)
    {
        var validator = new RegisterValidator();
        var cmd = new RegisterCommand(email, password, name);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
