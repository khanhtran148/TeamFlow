using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Application.Features.Sprints.CreateSprint;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Tests.RateLimiting;

/// <summary>
/// Integration tests that verify rate limiting returns 429 with ProblemDetails
/// and Retry-After header when limits are exceeded.
/// Note: The WebAppFactory overrides RateLimiting:WritePermitLimit to 3 for fast testing.
/// </summary>
[Collection("Integration")]
public sealed class RateLimitIntegrationTests(PostgresFixture postgres) : RateLimitTestBase(postgres)
{
    private const string SprintsUrl = "/api/v1/sprints";

    [Fact]
    public async Task WriteEndpoint_ExceedingLimit_Returns429()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        HttpResponseMessage? lastResponse = null;

        // Blast requests to exceed the write limit (set to 3 in RateLimitTestBase)
        for (var i = 0; i < 5; i++)
        {
            lastResponse = await client.PostAsJsonAsync(SprintsUrl,
                new CreateSprintCommand(projectId, $"Sprint {i}", null, null, null));

            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task WriteEndpoint_RateLimited_HasRetryAfterHeader()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        HttpResponseMessage? rateLimitedResponse = null;

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync(SprintsUrl,
                new CreateSprintCommand(projectId, $"Sprint RL {i}", null, null, null));

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        rateLimitedResponse.Should().NotBeNull();
        rateLimitedResponse!.Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Fact]
    public async Task WriteEndpoint_RateLimited_Returns429WithStructuredBody()
    {
        var projectId = await SeedProjectAsync(ProjectRole.OrgAdmin);
        var client = CreateAuthenticatedClient(ProjectRole.OrgAdmin);

        HttpResponseMessage? rateLimitedResponse = null;

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync(SprintsUrl,
                new CreateSprintCommand(projectId, $"Sprint Body {i}", null, null, null));

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimitedResponse = response;
                break;
            }
        }

        rateLimitedResponse.Should().NotBeNull();

        var body = await rateLimitedResponse!.Content.ReadFromJsonAsync<RateLimitResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be(429);
        body.Title.Should().NotBeNullOrEmpty();
    }

    /// <summary>Minimal deserialization target for 429 response body.</summary>
    private sealed record RateLimitResponse(string? Type, string? Title, int Status, string? Detail);
}
