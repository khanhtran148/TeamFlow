using System.Net;
using System.Text.Json;
using FluentAssertions;
using TeamFlow.Api.Tests.Infrastructure;

namespace TeamFlow.Api.Tests.Health;

[Collection("Integration")]
public sealed class HealthCheckTests(PostgresFixture postgres) : ApiIntegrationTestBase(postgres)
{
    [Fact]
    public async Task GetHealth_Returns200()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsJsonWithStatusAndChecks()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetString().Should().NotBeNullOrEmpty();

        root.TryGetProperty("checks", out var checksProp).Should().BeTrue();
        checksProp.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetHealth_DbCheckReportsHealthy()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        var checks = json.RootElement.GetProperty("checks");
        var hasDbCheck = false;

        foreach (var check in checks.EnumerateArray())
        {
            var name = check.GetProperty("name").GetString();
            if (name is not null && name.Contains("db", StringComparison.OrdinalIgnoreCase))
            {
                hasDbCheck = true;
                var status = check.GetProperty("status").GetString();
                status.Should().Be("Healthy");
            }
        }

        // If there's no explicit "db" check, the overall status should be Healthy
        if (!hasDbCheck)
        {
            var overallStatus = json.RootElement.GetProperty("status").GetString();
            overallStatus.Should().Be("Healthy");
        }
    }

    [Fact]
    public async Task GetHealthReady_Returns200()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
