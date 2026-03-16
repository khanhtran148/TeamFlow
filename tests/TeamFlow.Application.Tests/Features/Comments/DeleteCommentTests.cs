using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Comments.DeleteComment;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

public sealed class DeleteCommentTests
{
    private readonly ICommentRepository _commentRepo = Substitute.For<ICommentRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public DeleteCommentTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _commentRepo.UpdateAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Comment>());
    }

    private DeleteCommentHandler CreateHandler() =>
        new(_commentRepo, _workItemRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_OwnComment_SoftDeletes()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(UserId)
            .Build();

        _commentRepo.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new DeleteCommentCommand(comment.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        comment.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NotOwnComment_ReturnsForbidden()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(Guid.NewGuid())
            .Build();

        _commentRepo.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new DeleteCommentCommand(comment.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_ReturnsNotFound()
    {
        var comment = CommentBuilder.New().WithAuthor(UserId).Deleted().Build();
        _commentRepo.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);

        var cmd = new DeleteCommentCommand(comment.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }

    [Fact]
    public async Task Handle_NonExistentComment_ReturnsNotFound()
    {
        _commentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Comment?)null);

        var cmd = new DeleteCommentCommand(Guid.NewGuid());
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }
}
