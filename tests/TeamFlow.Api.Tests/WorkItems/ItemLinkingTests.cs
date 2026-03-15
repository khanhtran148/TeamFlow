using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.WorkItems.CheckBlockers;
using TeamFlow.Application.Features.WorkItems.CreateWorkItem;
using TeamFlow.Application.Features.WorkItems.GetLinks;
using TeamFlow.Application.Features.WorkItems.RemoveLink;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace TeamFlow.Api.Tests.WorkItems;

public sealed class ItemLinkingTests : IntegrationTestBase
{
    private ISender Sender => Services.GetRequiredService<ISender>();
    private TeamFlowDbContext DbCtx => Services.GetRequiredService<TeamFlowDbContext>();

    [Fact]
    public async Task AddLink_CreatesForwardAndReverseLink()
    {
        var projectId = await CreateTestProject();
        var itemA = await CreateTestItem(projectId, "Item A");
        var itemB = await CreateTestItem(projectId, "Item B");

        var result = await Sender.Send(new AddWorkItemLinkCommand(itemA, itemB, LinkType.RelatesTo));
        result.IsSuccess.Should().BeTrue();

        // Both links should exist in DB
        var linksInDb = await DbCtx.WorkItemLinks.CountAsync(l =>
            (l.SourceId == itemA && l.TargetId == itemB) ||
            (l.SourceId == itemB && l.TargetId == itemA));
        linksInDb.Should().Be(2);
    }

    [Fact]
    public async Task CircularBlock_Rejected()
    {
        var projectId = await CreateTestProject();
        var itemA = await CreateTestItem(projectId, "Item A");
        var itemB = await CreateTestItem(projectId, "Item B");

        // A blocks B
        await Sender.Send(new AddWorkItemLinkCommand(itemA, itemB, LinkType.Blocks));

        // B blocks A (circular) — should fail
        var circular = await Sender.Send(new AddWorkItemLinkCommand(itemB, itemA, LinkType.Blocks));
        circular.IsFailure.Should().BeTrue();
        circular.Error.Should().Contain("Circular");
    }

    [Fact]
    public async Task RemoveLink_RemovesBothDirections()
    {
        var projectId = await CreateTestProject();
        var itemA = await CreateTestItem(projectId, "Item A");
        var itemB = await CreateTestItem(projectId, "Item B");

        await Sender.Send(new AddWorkItemLinkCommand(itemA, itemB, LinkType.RelatesTo));

        // Find the forward link ID
        var link = await DbCtx.WorkItemLinks.FirstAsync(l => l.SourceId == itemA && l.TargetId == itemB);

        var removeResult = await Sender.Send(new RemoveWorkItemLinkCommand(link.Id));
        removeResult.IsSuccess.Should().BeTrue();

        // Both links removed
        var remainingLinks = await DbCtx.WorkItemLinks.CountAsync(l =>
            (l.SourceId == itemA && l.TargetId == itemB) ||
            (l.SourceId == itemB && l.TargetId == itemA));
        remainingLinks.Should().Be(0);
    }

    [Fact]
    public async Task CheckBlockers_ReturnsActiveblockers()
    {
        var projectId = await CreateTestProject();
        var blocker = await CreateTestItem(projectId, "Blocker");
        var blocked = await CreateTestItem(projectId, "Blocked Item");

        await Sender.Send(new AddWorkItemLinkCommand(blocker, blocked, LinkType.Blocks));

        var result = await Sender.Send(new CheckBlockersQuery(blocked));
        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeTrue();
        result.Value.Blockers.Should().HaveCount(1);
    }

    private async Task<Guid> CreateTestProject()
    {
        var result = await Sender.Send(new CreateProjectCommand(IntegrationTestBase.SeedOrgId, "Test Project", null));
        return result.Value.Id;
    }

    private async Task<Guid> CreateTestItem(Guid projectId, string title)
    {
        var result = await Sender.Send(new CreateWorkItemCommand(projectId, null, WorkItemType.Task, title, null, Priority.Medium, null));
        return result.Value.Id;
    }
}
