using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Comments.UpdateComment;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

public sealed class UpdateCommentTests
{
    private readonly ICommentRepository _commentRepo = Substitute.For<ICommentRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public UpdateCommentTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _commentRepo.UpdateAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Comment>());
    }

    private UpdateCommentHandler CreateHandler() =>
        new(_commentRepo, _workItemRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_OwnComment_UpdatesSuccessfully()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(UserId)
            .WithContent("Original")
            .Build();
        comment.Author = new User { Name = "Test User" };

        _commentRepo.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new UpdateCommentCommand(comment.Id, "Updated content");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Updated content");
        result.Value.EditedAt.Should().NotBeNull();
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

        var cmd = new UpdateCommentCommand(comment.Id, "Trying to edit");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_DeletedComment_ReturnsNotFound()
    {
        var comment = CommentBuilder.New().WithAuthor(UserId).Deleted().Build();
        _commentRepo.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);

        var cmd = new UpdateCommentCommand(comment.Id, "Updated");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }

    [Fact]
    public async Task Handle_NonExistentComment_ReturnsNotFound()
    {
        _commentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Comment?)null);

        var cmd = new UpdateCommentCommand(Guid.NewGuid(), "Updated");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyContent_Fails(string? content)
    {
        var validator = new UpdateCommentValidator();
        var cmd = new UpdateCommentCommand(Guid.NewGuid(), content!);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
