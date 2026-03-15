using FluentAssertions;
using TeamFlow.Api.RateLimiting;

namespace TeamFlow.Api.Tests.RateLimiting;

public sealed class AuthRateLimitTests
{
    [Fact]
    public void DefaultSettings_AuthPermitLimit_Is30()
    {
        var settings = new RateLimitSettings();

        settings.AuthPermitLimit.Should().Be(30, "Default: 30 auth req/min — blocks brute-force, allows dev/test");
    }

    [Fact]
    public void DefaultSettings_AuthWindow_Is60Seconds()
    {
        var settings = new RateLimitSettings();

        settings.AuthWindowSeconds.Should().Be(60);
    }

    [Fact]
    public void DefaultSettings_SlidingWindow_Has4Segments()
    {
        var settings = new RateLimitSettings();

        settings.SegmentsPerWindow.Should().Be(4, "Sliding window with 4 segments for smoother limiting");
    }

    [Theory]
    [InlineData(nameof(RateLimitSettings.WritePermitLimit), 60)]
    [InlineData(nameof(RateLimitSettings.SearchPermitLimit), 40)]
    [InlineData(nameof(RateLimitSettings.BulkPermitLimit), 10)]
    [InlineData(nameof(RateLimitSettings.GeneralPermitLimit), 200)]
    public void DefaultSettings_PolicyLimits_AreReasonable(string propertyName, int expectedLimit)
    {
        var settings = new RateLimitSettings();
        var actual = (int)typeof(RateLimitSettings).GetProperty(propertyName)!.GetValue(settings)!;

        actual.Should().Be(expectedLimit);
    }
}
