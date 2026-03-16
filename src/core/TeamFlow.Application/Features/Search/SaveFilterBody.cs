using System.Text.Json;

namespace TeamFlow.Application.Features.Search;

public sealed record SaveFilterBody(string Name, JsonDocument FilterJson, bool IsDefault);
public sealed record UpdateSavedFilterBody(string? Name, JsonDocument? FilterJson, bool? IsDefault);
