using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.DeleteRetroSession;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class DeleteRetroSessionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidSession_DeletesSuccessfully()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var command = new DeleteRetroSessionCommand(session.Id);
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeTrue();
        var exists = await DbContext.Set<TeamFlow.Domain.Entities.RetroSession>()
            .AnyAsync(s => s.Id == session.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        var command = new DeleteRetroSessionCommand(Guid.NewGuid());
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Retro session not found");
    }
}

[Collection("Social")]
public sealed class DeleteRetroSessionForbiddenTests(PostgresCollectionFixture fixture)
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

        var command = new DeleteRetroSessionCommand(session.Id);
        var result = await Sender.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
    }
}
