using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.WorkItems.AssignWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class AssignWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidAssignment_SetsAssignee()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AssignWorkItemCommand(item.Id, assignee.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == item.Id);
        updated.AssigneeId.Should().Be(assignee.Id);
    }

    [Fact]
    public async Task Handle_AssignEpic_ReturnsValidationError()
    {
        var project = await SeedProjectAsync();
        var epic = await SeedWorkItemAsync(project.Id, b => b.AsEpic());
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AssignWorkItemCommand(epic.Id, assignee.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Epic");
    }

    [Fact]
    public async Task Handle_InvalidUser_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new AssignWorkItemCommand(item.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_Assignment_RecordsHistory()
    {
        var project = await SeedProjectAsync();
        var prevAssignee = UserBuilder.New().Build();
        DbContext.Users.Add(prevAssignee);
        await DbContext.SaveChangesAsync();

        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithAssignee(prevAssignee.Id));

        var newAssignee = UserBuilder.New().Build();
        DbContext.Users.Add(newAssignee);
        await DbContext.SaveChangesAsync();

        await Sender.Send(new AssignWorkItemCommand(item.Id, newAssignee.Id));

        DbContext.ChangeTracker.Clear();
        var historyEntry = await DbContext.Set<Domain.Entities.WorkItemHistory>()
            .Where(h => h.WorkItemId == item.Id && h.FieldName == "AssigneeId")
            .FirstOrDefaultAsync();
        historyEntry.Should().NotBeNull();
        historyEntry!.OldValue.Should().Be(prevAssignee.Id.ToString());
    }

    [Fact]
    public async Task Handle_ValidAssignment_SetsAssignedAt()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var beforeAssign = DateTime.UtcNow;
        await Sender.Send(new AssignWorkItemCommand(item.Id, assignee.Id));

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == item.Id);
        updated.AssignedAt.Should().NotBeNull();
        updated.AssignedAt.Should().BeOnOrAfter(beforeAssign);
        updated.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_Reassignment_UpdatesAssignedAt()
    {
        var project = await SeedProjectAsync();
        var originalAssignee = UserBuilder.New().Build();
        DbContext.Users.Add(originalAssignee);
        await DbContext.SaveChangesAsync();

        var originalAssignedAt = DateTime.UtcNow.AddDays(-7);
        var item = await SeedWorkItemAsync(project.Id,
            b => b.AsTask().WithAssignee(originalAssignee.Id).WithAssignedAt(originalAssignedAt));

        var newAssignee = UserBuilder.New().Build();
        DbContext.Users.Add(newAssignee);
        await DbContext.SaveChangesAsync();

        var beforeReassign = DateTime.UtcNow;
        await Sender.Send(new AssignWorkItemCommand(item.Id, newAssignee.Id));

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == item.Id);
        updated.AssignedAt.Should().BeOnOrAfter(beforeReassign);
    }
}

[Collection("WorkItems")]
public sealed class AssignWorkItemPermissionTests(PostgresCollectionFixture fixture)
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
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var assignee = UserBuilder.New().Build();
        DbContext.Users.Add(assignee);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AssignWorkItemCommand(item.Id, assignee.Id));

        result.IsFailure.Should().BeTrue();
    }
}
