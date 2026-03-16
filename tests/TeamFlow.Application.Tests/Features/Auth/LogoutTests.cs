using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Auth.Logout;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Auth;

[Collection("Auth")]
public sealed class LogoutTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_RevokesAllRefreshTokensForUser()
    {
        DbContext.Set<RefreshToken>().AddRange(
            new RefreshToken
            {
                UserId = SeedUserId,
                TokenHash = "hash-1",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            },
            new RefreshToken
            {
                UserId = SeedUserId,
                TokenHash = "hash-2",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            }
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new LogoutCommand());

        result.IsSuccess.Should().BeTrue();

        var activeTokens = await DbContext.Set<RefreshToken>()
            .Where(t => t.UserId == SeedUserId && t.RevokedAt == null)
            .CountAsync();
        activeTokens.Should().Be(0);
    }
}
