using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.RefreshToken;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Auth;

public sealed class RefreshTokenTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    private static readonly User TestUser = new()
    {
        Email = "user@test.com",
        Name = "Test User",
        PasswordHash = "hashed-password"
    };

    public RefreshTokenTests()
    {
        _authService.GenerateJwt(Arg.Any<User>()).Returns("new-jwt-token");
        _authService.GenerateRefreshToken().Returns("new-refresh-token");
        _authService.HashToken("old-refresh-token").Returns("hashed-old-token");
        _authService.HashToken("new-refresh-token").Returns("hashed-new-token");
    }

    private RefreshTokenHandler CreateHandler() => new(_refreshTokenRepo, _userRepo, _authService);

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokens()
    {
        var existingToken = new Domain.Entities.RefreshToken
        {
            UserId = TestUser.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _refreshTokenRepo.GetByTokenHashAsync("hashed-old-token", Arg.Any<CancellationToken>()).Returns(existingToken);
        _userRepo.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);

        var cmd = new RefreshTokenCommand("old-refresh-token");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-jwt-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesOldToken()
    {
        var existingToken = new Domain.Entities.RefreshToken
        {
            UserId = TestUser.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _refreshTokenRepo.GetByTokenHashAsync("hashed-old-token", Arg.Any<CancellationToken>()).Returns(existingToken);
        _userRepo.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);

        await CreateHandler().Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None);

        existingToken.RevokedAt.Should().NotBeNull();
        existingToken.ReplacedByTokenHash.Should().Be("hashed-new-token");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        var expiredToken = new Domain.Entities.RefreshToken
        {
            UserId = TestUser.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // expired
        };
        _refreshTokenRepo.GetByTokenHashAsync("hashed-old-token", Arg.Any<CancellationToken>()).Returns(expiredToken);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsFailure()
    {
        var revokedToken = new Domain.Entities.RefreshToken
        {
            UserId = TestUser.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddHours(-1) // revoked
        };
        _refreshTokenRepo.GetByTokenHashAsync("hashed-old-token", Arg.Any<CancellationToken>()).Returns(revokedToken);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentToken_ReturnsFailure()
    {
        _refreshTokenRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.RefreshToken?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
