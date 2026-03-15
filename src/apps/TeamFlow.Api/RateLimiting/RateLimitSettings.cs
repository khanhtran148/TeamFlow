namespace TeamFlow.Api.RateLimiting;

/// <summary>
/// Rate limiting configuration — loaded from appsettings.json section "RateLimiting".
/// Supports runtime changes via IOptionsMonitor.
/// </summary>
public sealed class RateLimitSettings
{
    public int AuthPermitLimit { get; set; } = 30;
    public int AuthWindowSeconds { get; set; } = 60;
    public int WritePermitLimit { get; set; } = 60;
    public int WriteWindowSeconds { get; set; } = 60;
    public int SearchPermitLimit { get; set; } = 40;
    public int SearchWindowSeconds { get; set; } = 60;
    public int BulkPermitLimit { get; set; } = 10;
    public int BulkWindowSeconds { get; set; } = 60;
    public int GeneralPermitLimit { get; set; } = 200;
    public int GeneralWindowSeconds { get; set; } = 60;
    public int SegmentsPerWindow { get; set; } = 4;
    public int QueueLimit { get; set; } = 0;
}
