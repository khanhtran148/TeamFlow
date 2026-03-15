using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// Shared JSON serializer options matching the API's configuration.
/// The API uses camelCase naming + JsonStringEnumConverter.
/// </summary>
internal static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
