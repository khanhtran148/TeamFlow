using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.RenameRetroSession;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class RenameRetroSessionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidName_RenamesSession()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .WithStatus(RetroSessionStatus.Open)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var command = new RenameRetroSessionCommand(session.Id, "New Name");
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeTrue();
        var persisted = await DbContext.Set<TeamFlow.Domain.Entities.RetroSession>()
            .SingleAsync(s => s.Id == session.Id);
        persisted.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        var command = new RenameRetroSessionCommand(Guid.NewGuid(), "New Name");
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Retro session not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_EmptyOrWhitespaceName_ReturnsFailure(string? name)
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var command = new RenameRetroSessionCommand(session.Id, name!);
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Name is required");
    }

    [Fact]
    public async Task Handle_NameWithWhitespace_TrimsBeforeSaving()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var command = new RenameRetroSessionCommand(session.Id, "  Trimmed Name  ");
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeTrue();
        var persisted = await DbContext.Set<TeamFlow.Domain.Entities.RetroSession>()
            .SingleAsync(s => s.Id == session.Id);
        persisted.Name.Should().Be("Trimmed Name");
    }
}

[Collection("Social")]
public sealed class RenameRetroSessionForbiddenTests(PostgresCollectionFixture fixture)
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

        var command = new RenameRetroSessionCommand(session.Id, "New Name");
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }
}
