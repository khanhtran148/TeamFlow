using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.WorkItems.RemoveLink;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class RemoveLinkTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingLink_RemovesBothDirections()
    {
        var project = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var itemB = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        // Create the bidirectional link pair
        await Sender.Send(new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo));
        DbContext.ChangeTracker.Clear();

        // Find the forward link to get its ID
        var forwardLink = await DbContext.Set<Domain.Entities.WorkItemLink>()
            .AsNoTracking()
            .FirstAsync(l => l.SourceId == itemA.Id && l.TargetId == itemB.Id);

        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new RemoveWorkItemLinkCommand(forwardLink.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var remaining = await DbContext.Set<Domain.Entities.WorkItemLink>()
            .AsNoTracking()
            .Where(l => (l.SourceId == itemA.Id && l.TargetId == itemB.Id)
                     || (l.SourceId == itemB.Id && l.TargetId == itemA.Id))
            .ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonExistentLink_ReturnsNotFound()
    {
        var result = await Sender.Send(new RemoveWorkItemLinkCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
