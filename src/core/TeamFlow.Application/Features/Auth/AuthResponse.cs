namespace TeamFlow.Application.Features.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    bool MustChangePassword = false);
