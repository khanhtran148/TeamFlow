using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Backlog.BulkUpdatePriority;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

public sealed class BulkUpdatePriorityTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public BulkUpdatePriorityTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private BulkUpdatePriorityHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions);

    [Fact]
    public async Task Handle_MultipleItems_UpdatesAll()
    {
        var wi1 = WorkItemBuilder.New().WithProject(ProjectId).WithPriority(Priority.Low).Build();
        var wi2 = WorkItemBuilder.New().WithProject(ProjectId).WithPriority(Priority.Low).Build();
        var wi3 = WorkItemBuilder.New().WithProject(ProjectId).WithPriority(Priority.Low).Build();

        _workItemRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<IEnumerable<Guid>>().ToList();
                var all = new[] { wi1, wi2, wi3 };
                return (IReadOnlyList<WorkItem>)all.Where(w => ids.Contains(w.Id)).ToList();
            });

        var cmd = new BulkUpdatePriorityCommand([
            new PriorityUpdate(wi1.Id, Priority.High),
            new PriorityUpdate(wi2.Id, Priority.Critical),
            new PriorityUpdate(wi3.Id, Priority.Medium)
        ]);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        wi1.Priority.Should().Be(Priority.High);
        wi2.Priority.Should().Be(Priority.Critical);
        wi3.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public async Task Handle_MissingItem_ReturnsFailure()
    {
        _workItemRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<WorkItem>)new List<WorkItem>());

        var cmd = new BulkUpdatePriorityCommand([
            new PriorityUpdate(Guid.NewGuid(), Priority.High)
        ]);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_PartialPermissionFailure_ReturnsFailure()
    {
        var wi1 = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var wi2 = WorkItemBuilder.New().WithProject(Guid.NewGuid()).Build();

        _workItemRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<IEnumerable<Guid>>().ToList();
                var all = new[] { wi1, wi2 };
                return (IReadOnlyList<WorkItem>)all.Where(w => ids.Contains(w.Id)).ToList();
            });
        _permissions.HasPermissionAsync(UserId, wi2.ProjectId, Permission.WorkItem_Edit, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new BulkUpdatePriorityCommand([
            new PriorityUpdate(wi1.Id, Priority.High),
            new PriorityUpdate(wi2.Id, Priority.Critical)
        ]);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
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
