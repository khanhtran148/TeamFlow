using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Respawn;
using TeamFlow.Api.Tests.Infrastructure;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.RateLimiting;

/// <summary>
/// Base class for rate-limiting integration tests.
/// Uses a custom WebAppFactory that overrides rate limit settings to low values
/// so tests can trigger 429 responses without sending dozens of requests.
/// </summary>
[Collection("Integration")]
public abstract class RateLimitTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private RateLimitTestWebAppFactory _factory = null!;
    private Checkpoint? _checkpoint;

    protected static readonly Guid SeedOrgId = IntegrationTestBase.SeedOrgId;
    protected static readonly Guid SeedUserId = IntegrationTestBase.SeedUserId;

    protected RateLimitTestBase(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        _factory = new RateLimitTestWebAppFactory(_postgres);
        _ = _factory.Server;

        await _factory.EnsureDatabaseAsync();

        _checkpoint = new Checkpoint
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        };

        await using var conn = new NpgsqlConnection(_postgres.ConnectionString);
        await conn.OpenAsync();
        await _checkpoint.Reset(conn);
        await _factory.SeedReferenceDataAsync();
    }

    protected HttpClient CreateAuthenticatedClient(ProjectRole role = ProjectRole.Developer)
    {
        var token = GenerateTestJwt(SeedUserId, "test@teamflow.dev", "Test User");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected async Task<Guid> SeedProjectAsync(
        ProjectRole role = ProjectRole.Developer,
        Guid? userId = null)
    {
        userId ??= SeedUserId;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();

        var project = new Project
        {
            OrgId = SeedOrgId,
            Name = "Rate Limit Test Project",
            Description = "Seeded for rate limit tests"
        };
        db.Set<Project>().Add(project);

        var membership = new ProjectMembership
        {
            ProjectId = project.Id,
            MemberId = userId.Value,
            MemberType = "User",
            Role = role
        };
        db.Set<ProjectMembership>().Add(membership);

        // Seed OrgAdmin anchor when not OrgAdmin to disable bootstrap
        if (role != ProjectRole.OrgAdmin)
        {
            if (!await db.Set<User>().AnyAsync(u => u.Id == new Guid("00000000-0000-0000-0000-000000000002")))
            {
                var user2 = new User
                {
                    Email = "admin@teamflow.dev",
                    Name = "Admin User",
                    PasswordHash = "not-a-real-hash"
                };
                db.Entry(user2).Property(nameof(User.Id)).CurrentValue = new Guid("00000000-0000-0000-0000-000000000002");
                db.Set<User>().Add(user2);
            }

            db.Set<ProjectMembership>().Add(new ProjectMembership
            {
                ProjectId = project.Id,
                MemberId = new Guid("00000000-0000-0000-0000-000000000002"),
                MemberType = "User",
                Role = ProjectRole.OrgAdmin
            });
        }

        await db.SaveChangesAsync();
        return project.Id;
    }

    public async Task DisposeAsync()
    {
        if (_checkpoint is not null)
        {
            await using var conn = new NpgsqlConnection(_postgres.ConnectionString);
            await conn.OpenAsync();
            await _checkpoint.Reset(conn);
        }

        await _factory.DisposeAsync();
    }

    private static string GenerateTestJwt(Guid userId, string email, string name)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(TestJwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("name", name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtSettings.Issuer,
            audience: TestJwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
