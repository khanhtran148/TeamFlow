using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Tests;

internal static class TestDbContextFactory
{
    public static TeamFlowDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TeamFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestTeamFlowDbContext(options);
        context.Database.EnsureCreated();

        // Disable FK enforcement for unit tests -- avoids needing entire entity graph
        context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");

        return context;
    }
}

/// <summary>
/// Test-specific DbContext that replaces PostgreSQL jsonb column types with SQLite-compatible text
/// and adds value converters for JsonDocument properties.
/// </summary>
internal sealed class TestTeamFlowDbContext : TeamFlowDbContext
{
    public TestTeamFlowDbContext(DbContextOptions<TeamFlowDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var jsonDocConverter = new ValueConverter<JsonDocument, string>(
            v => v.RootElement.GetRawText(),
            v => JsonDocument.Parse(v, default));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // JsonDocument is a reference type so nullable and non-nullable share the same ClrType
                if (property.ClrType == typeof(JsonDocument))
                {
                    property.SetValueConverter(jsonDocConverter);
                    property.SetColumnType(null);
                }

                // Remove any PostgreSQL-specific column types that SQLite doesn't understand
                var columnType = property.GetColumnType();
                if (columnType is "jsonb" or "timestamptz" or "vector(1536)")
                {
                    property.SetColumnType(null);
                }
            }
        }
    }
}
