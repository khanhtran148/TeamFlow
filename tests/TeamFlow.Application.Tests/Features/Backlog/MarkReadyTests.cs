using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Backlog.MarkReadyForSprint;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

[Collection("WorkItems")]
public sealed class MarkReadyTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidItem_MarksReady()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new MarkReadyForSprintCommand(workItem.Id, true));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == workItem.Id);
        updated.IsReadyForSprint.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidItem_UnmarksReady()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        workItem.IsReadyForSprint = true;
        DbContext.WorkItems.Update(workItem);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new MarkReadyForSprintCommand(workItem.Id, false));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == workItem.Id);
        updated.IsReadyForSprint.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new MarkReadyForSprintCommand(Guid.NewGuid(), true));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_RecordsHistory()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        await Sender.Send(new MarkReadyForSprintCommand(workItem.Id, true));

        DbContext.ChangeTracker.Clear();
        var historyEntry = await DbContext.Set<Domain.Entities.WorkItemHistory>()
            .Where(h => h.WorkItemId == workItem.Id && h.FieldName == "IsReadyForSprint")
            .FirstOrDefaultAsync();
        historyEntry.Should().NotBeNull();
    }
}

[Collection("WorkItems")]
public sealed class MarkReadyPermissionTests(PostgresCollectionFixture fixture)
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
        var workItem = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new MarkReadyForSprintCommand(workItem.Id, true));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
