using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.CreateRetroActionItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class CreateRetroActionItemTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public CreateRetroActionItemTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _retroRepo.AddActionItemAsync(Arg.Any<RetroActionItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroActionItem>());
    }

    private CreateRetroActionItemHandler CreateHandler() =>
        new(_retroRepo, _workItemRepo, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidCommand_CreatesActionItem()
    {
        var session = RetroSessionBuilder.New().WithProject(ProjectId).Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var cmd = new CreateRetroActionItemCommand(session.Id, null, "Fix CI pipeline", "Description", null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Fix CI pipeline");
    }

    [Fact]
    public async Task Handle_WithBacklogLink_CreatesWorkItem()
    {
        var session = RetroSessionBuilder.New().WithProject(ProjectId).Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _workItemRepo.AddAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var wi = ci.Arg<WorkItem>();
                return wi;
            });

        var cmd = new CreateRetroActionItemCommand(session.Id, null, "Fix CI", null, null, null, true);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedTaskId.Should().NotBeNull();
        await _workItemRepo.Received(1).AddAsync(
            Arg.Is<WorkItem>(w => w.Type == WorkItemType.Task && w.Title == "Fix CI"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutBacklogLink_DoesNotCreateWorkItem()
    {
        var session = RetroSessionBuilder.New().WithProject(ProjectId).Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var cmd = new CreateRetroActionItemCommand(session.Id, null, "Fix CI", null, null, null, false);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkedTaskId.Should().BeNull();
        await _workItemRepo.DidNotReceive().AddAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyTitle_Fails(string? title)
    {
        var validator = new CreateRetroActionItemValidator();
        var cmd = new CreateRetroActionItemCommand(Guid.NewGuid(), null, title!, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
