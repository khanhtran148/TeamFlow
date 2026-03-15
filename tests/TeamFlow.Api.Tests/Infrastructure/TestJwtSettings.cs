namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// JWT settings used by both the WebApplicationFactory and test JWT generation.
/// Must match what the factory configures so tokens are accepted by the auth middleware.
/// </summary>
public static class TestJwtSettings
{
    public const string Issuer = "TeamFlow";
    public const string Audience = "TeamFlow.Users";

    /// <summary>
    /// 64-character test-only secret. Never used in production.
    /// Must be at least 32 bytes for HMAC-SHA256.
    /// </summary>
    public const string Secret = "ThisIsATestSecretKeyForIntegrationTestsOnly_MustBe64CharsLong!!";
}
