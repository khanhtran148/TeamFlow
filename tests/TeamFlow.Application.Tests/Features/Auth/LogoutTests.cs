using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.Logout;

namespace TeamFlow.Application.Tests.Features.Auth;

public sealed class LogoutTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private static readonly Guid UserId = Guid.NewGuid();

    public LogoutTests()
    {
        _currentUser.Id.Returns(UserId);
    }

    private LogoutHandler CreateHandler() => new(_refreshTokenRepo, _currentUser);

    [Fact]
    public async Task Handle_RevokesAllRefreshTokensForUser()
    {
        var result = await CreateHandler().Handle(new LogoutCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepo.Received(1).RevokeAllForUserAsync(UserId, Arg.Any<CancellationToken>());
    }
}
