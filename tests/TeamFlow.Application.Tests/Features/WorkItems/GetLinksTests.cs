using FluentAssertions;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.WorkItems.GetLinks;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class GetLinksTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ItemWithLinks_ReturnsGroupedLinks()
    {
        var project = await SeedProjectAsync();
        var itemA = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.Task));
        var itemB = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.Bug));

        await Sender.Send(new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.Blocks));
        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new GetWorkItemLinksQuery(itemA.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Groups.Should().NotBeEmpty();
        result.Value.Groups.Should().Contain(g => g.LinkType == LinkType.Blocks);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new GetWorkItemLinksQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
