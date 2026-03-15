using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Respawn;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;

namespace TeamFlow.Api.Tests.Infrastructure;

/// <summary>
/// Base class for HTTP-level integration tests.
/// Uses Respawn to reset the database between tests, then re-seeds reference data.
/// Provides helpers to create authenticated and anonymous HttpClients.
/// </summary>
[Collection("Integration")]
public abstract class ApiIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private IntegrationTestWebAppFactory _factory = null!;
    private Checkpoint? _checkpoint;

    /// <summary>Well-known seed Organization ID, matching IntegrationTestBase.</summary>
    protected static readonly Guid SeedOrgId = IntegrationTestBase.SeedOrgId;

    /// <summary>Well-known seed User ID, matching IntegrationTestBase.</summary>
    protected static readonly Guid SeedUserId = IntegrationTestBase.SeedUserId;

    /// <summary>A second user ID for multi-user test scenarios.</summary>
    protected static readonly Guid SeedUser2Id = new("00000000-0000-0000-0000-000000000002");

    protected ApiIntegrationTestBase(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestWebAppFactory(_postgres);

        // Force the factory to build the host so EnsureCreated can run
        _ = _factory.Server;

        await _factory.EnsureDatabaseAsync();

        // Initialize Respawn Checkpoint
        _checkpoint = new Checkpoint
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"]
        };

        // Reset and re-seed
        await using var conn = new NpgsqlConnection(_postgres.ConnectionString);
        await conn.OpenAsync();
        await _checkpoint.Reset(conn);
        await _factory.SeedReferenceDataAsync();
    }

    /// <summary>
    /// Creates an HttpClient with a valid JWT Bearer token for the seeded user
    /// with a project membership at the given role.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(ProjectRole role = ProjectRole.Developer)
    {
        var token = GenerateTestJwt(SeedUserId, "test@teamflow.dev", "Test User");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient without any authentication header.
    /// </summary>
    protected HttpClient CreateAnonymousClient()
    {
        return _factory.CreateClient();
    }

    /// <summary>
    /// Seeds an Organization + Project + ProjectMembership for the given user and role.
    /// When the requested role is NOT OrgAdmin, also seeds an OrgAdmin membership for
    /// SeedUser2Id so the PermissionChecker bootstrap logic is disabled.
    /// Returns the project ID.
    /// </summary>
    protected async Task<Guid> SeedProjectAsync(
        ProjectRole role = ProjectRole.Developer,
        Guid? userId = null,
        string projectName = "Test Project")
    {
        userId ??= SeedUserId;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();

        // Ensure User2 exists for OrgAdmin anchor
        if (!await db.Set<User>().AnyAsync(u => u.Id == SeedUser2Id))
        {
            var user2 = new User
            {
                Email = "admin@teamflow.dev",
                Name = "Admin User",
                PasswordHash = "not-a-real-hash"
            };
            db.Entry(user2).Property(nameof(User.Id)).CurrentValue = SeedUser2Id;
            db.Set<User>().Add(user2);
        }

        var project = new Project
        {
            OrgId = SeedOrgId,
            Name = projectName,
            Description = "Seeded for integration tests"
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

        // When not OrgAdmin, seed an OrgAdmin anchor so PermissionChecker
        // bootstrap logic ("no admin exists => allow all") is disabled.
        if (role != ProjectRole.OrgAdmin)
        {
            var adminMembership = new ProjectMembership
            {
                ProjectId = project.Id,
                MemberId = SeedUser2Id,
                MemberType = "User",
                Role = ProjectRole.OrgAdmin
            };
            db.Set<ProjectMembership>().Add(adminMembership);
        }

        await db.SaveChangesAsync();
        return project.Id;
    }

    /// <summary>
    /// Provides direct DB access for test assertions that need to verify persistence.
    /// </summary>
    protected async Task<T> WithDbContextAsync<T>(Func<TeamFlowDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamFlowDbContext>();
        return await action(db);
    }

    public async Task DisposeAsync()
    {
        // Reset DB for next test (defensive - InitializeAsync also resets)
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
