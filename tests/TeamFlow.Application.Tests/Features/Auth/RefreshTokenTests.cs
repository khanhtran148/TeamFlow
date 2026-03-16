using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.RefreshToken;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Auth;

[Collection("Auth")]
public sealed class RefreshTokenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _authService.GenerateJwt(Arg.Any<User>()).Returns("new-jwt-token");
        _authService.GenerateRefreshToken().Returns("new-refresh-token");
        _authService.HashToken("old-refresh-token").Returns("hashed-old-token");
        _authService.HashToken("new-refresh-token").Returns("hashed-new-token");
        services.AddSingleton(_authService);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokens()
    {
        var user = UserBuilder.New().WithEmail("refresh-valid@test.com").Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RefreshTokenCommand("old-refresh-token"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-jwt-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesOldToken()
    {
        var user = UserBuilder.New().WithEmail("refresh-revoke@test.com").Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        var oldToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        DbContext.Set<RefreshToken>().Add(oldToken);
        await DbContext.SaveChangesAsync();

        await Sender.Send(new RefreshTokenCommand("old-refresh-token"));

        var persisted = await DbContext.Set<RefreshToken>()
            .Where(t => t.TokenHash == "hashed-old-token" && t.UserId == user.Id)
            .SingleAsync();
        persisted.RevokedAt.Should().NotBeNull();
        persisted.ReplacedByTokenHash.Should().Be("hashed-new-token");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        var user = UserBuilder.New().WithEmail("refresh-expired@test.com").Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RefreshTokenCommand("old-refresh-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsFailure()
    {
        var user = UserBuilder.New().WithEmail("refresh-revoked@test.com").Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RefreshTokenCommand("old-refresh-token"));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentToken_ReturnsFailure()
    {
        var result = await Sender.Send(new RefreshTokenCommand("unknown-token"));

        result.IsFailure.Should().BeTrue();
    }
}
