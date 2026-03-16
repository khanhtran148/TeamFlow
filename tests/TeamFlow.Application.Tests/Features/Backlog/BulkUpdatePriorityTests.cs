using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.Backlog.BulkUpdatePriority;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

[Collection("WorkItems")]
public sealed class BulkUpdatePriorityTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_MultipleItems_UpdatesAll()
    {
        var project = await SeedProjectAsync();
        var wi1 = await SeedWorkItemAsync(project.Id, b => b.WithPriority(Priority.Low));
        var wi2 = await SeedWorkItemAsync(project.Id, b => b.WithPriority(Priority.Low));
        var wi3 = await SeedWorkItemAsync(project.Id, b => b.WithPriority(Priority.Low));

        var cmd = new BulkUpdatePriorityCommand([
            new PriorityUpdate(wi1.Id, Priority.High),
            new PriorityUpdate(wi2.Id, Priority.Critical),
            new PriorityUpdate(wi3.Id, Priority.Medium)
        ]);

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var updated1 = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == wi1.Id);
        var updated2 = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == wi2.Id);
        var updated3 = await DbContext.WorkItems.AsNoTracking().FirstAsync(w => w.Id == wi3.Id);
        updated1.Priority.Should().Be(Priority.High);
        updated2.Priority.Should().Be(Priority.Critical);
        updated3.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public async Task Handle_MissingItem_ReturnsFailure()
    {
        var cmd = new BulkUpdatePriorityCommand([
            new PriorityUpdate(Guid.NewGuid(), Priority.High)
        ]);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Validate_EmptyList_Fails()
    {
        var validator = new BulkUpdatePriorityValidator();
        var cmd = new BulkUpdatePriorityCommand([]);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}

[Collection("WorkItems")]
public sealed class BulkUpdatePriorityPermissionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Application.Common.Interfaces.IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var wi1 = await SeedWorkItemAsync(project.Id, b => b.WithPriority(Priority.Low));

        var cmd = new BulkUpdatePriorityCommand([
            new PriorityUpdate(wi1.Id, Priority.High)
        ]);

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
