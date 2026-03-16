using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class AddLinkTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidLink_CreatesBidirectionalPair()
    {
        var project = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var itemB = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var cmd = new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var links = await DbContext.Set<Domain.Entities.WorkItemLink>()
            .Where(l => (l.SourceId == itemA.Id && l.TargetId == itemB.Id)
                     || (l.SourceId == itemB.Id && l.TargetId == itemA.Id))
            .ToListAsync();
        links.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_DuplicateLink_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var itemB = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        // Create the link first
        await Sender.Send(new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo));

        DbContext.ChangeTracker.Clear();

        // Try to add the same link again
        var result = await Sender.Send(new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_CircularBlock_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var itemB = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        // B blocks A first
        await Sender.Send(new AddWorkItemLinkCommand(itemB.Id, itemA.Id, LinkType.Blocks));
        DbContext.ChangeTracker.Clear();

        // Now try A blocks B — should be circular
        var result = await Sender.Send(new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.Blocks));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Circular");
    }

    [Fact]
    public async Task Handle_CrossProjectLink_SetsScopeCorrectly()
    {
        var projectA = await SeedProjectAsync();
        var projectB = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(projectA.Id, b => b.AsTask());
        var itemB = await SeedWorkItemAsync(projectB.Id, b => b.AsTask());

        var cmd = new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var links = await DbContext.Set<Domain.Entities.WorkItemLink>()
            .Where(l => l.SourceId == itemA.Id && l.TargetId == itemB.Id)
            .ToListAsync();
        links.Should().ContainSingle();
        links.First().Scope.Should().Be(LinkScope.CrossProject);
    }

    [Fact]
    public async Task Handle_MissingSourceItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new AddWorkItemLinkCommand(Guid.NewGuid(), Guid.NewGuid(), LinkType.RelatesTo));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("WorkItems")]
public sealed class AddLinkPermissionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Application.Common.Interfaces.IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var itemB = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
