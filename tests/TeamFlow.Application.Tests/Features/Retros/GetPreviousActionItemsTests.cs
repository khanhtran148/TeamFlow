using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.GetPreviousActionItems;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class GetPreviousActionItemsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithClosedSession_ReturnsActionItems()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .WithStatus(RetroSessionStatus.Closed)
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var actionItem1 = new RetroActionItem { Title = "Fix CI", SessionId = session.Id };
        var actionItem2 = new RetroActionItem { Title = "Update docs", SessionId = session.Id };
        DbContext.Set<RetroActionItem>().AddRange(actionItem1, actionItem2);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetPreviousActionItemsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoPreviousSessions_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetPreviousActionItemsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PreviousSessionWithNoActionItems_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .WithStatus(RetroSessionStatus.Closed)
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetPreviousActionItemsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}

[Collection("Social")]
public sealed class GetPreviousActionItemsForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoRetroViewPermission_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetPreviousActionItemsQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
