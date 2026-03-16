using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.UpdateColumnsConfig;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class UpdateColumnsConfigTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidConfig_UpdatesSession()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var config = JsonDocument.Parse("""[{"name":"Went Well","color":"green"},{"name":"To Improve","color":"red"}]""");
        var command = new UpdateColumnsConfigCommand(session.Id, config);
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        var config = JsonDocument.Parse("[]");
        var command = new UpdateColumnsConfigCommand(Guid.NewGuid(), config);
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Retro session not found");
    }
}

[Collection("Social")]
public sealed class UpdateColumnsConfigForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var config = JsonDocument.Parse("[]");
        var command = new UpdateColumnsConfigCommand(session.Id, config);
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }
}
