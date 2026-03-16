using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Comments.CreateComment;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

public sealed class CreateCommentTests
{
    private readonly ICommentRepository _commentRepo = Substitute.For<ICommentRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public CreateCommentTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _commentRepo.AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Comment>());
        _userRepo.GetByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new User { Name = "Test User" });
    }

    private CreateCommentHandler CreateHandler() =>
        new(_commentRepo, _workItemRepo, _userRepo, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidCommand_CreatesComment()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new CreateCommentCommand(workItem.Id, "Great work!", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Great work!");
        result.Value.AuthorId.Should().Be(UserId);
        result.Value.WorkItemId.Should().Be(workItem.Id);
    }

    [Fact]
    public async Task Handle_InvalidWorkItem_ReturnsNotFound()
    {
        _workItemRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        var cmd = new CreateCommentCommand(Guid.NewGuid(), "Comment", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Work item not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Comment_Create, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new CreateCommentCommand(workItem.Id, "Comment", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_ParentNotFound_ReturnsFailure()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _commentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Comment?)null);

        var cmd = new CreateCommentCommand(workItem.Id, "Reply", Guid.NewGuid());
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Parent comment not found");
    }

    [Fact]
    public async Task Handle_NestedReply_ReturnsFailure()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var parentComment = CommentBuilder.New().WithWorkItem(workItem.Id).WithParent(Guid.NewGuid()).Build();
        _commentRepo.GetByIdAsync(parentComment.Id, Arg.Any<CancellationToken>()).Returns(parentComment);

        var cmd = new CreateCommentCommand(workItem.Id, "Nested reply", parentComment.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Nested replies are not allowed");
    }

    [Fact]
    public async Task Handle_DeletedParent_ReturnsFailure()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var deletedParent = CommentBuilder.New().WithWorkItem(workItem.Id).Deleted().Build();
        _commentRepo.GetByIdAsync(deletedParent.Id, Arg.Any<CancellationToken>()).Returns(deletedParent);

        var cmd = new CreateCommentCommand(workItem.Id, "Reply to deleted", deletedParent.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Parent comment not found");
    }

    [Fact]
    public async Task Handle_WithMentions_PublishesMentionEvents()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var mentionedUser = new User { Name = "John.Doe" };
        _userRepo.GetByDisplayNamesAsync(
            Arg.Is<IEnumerable<string>>(n => n.Contains("John.Doe")),
            Arg.Any<CancellationToken>())
            .Returns([mentionedUser]);

        var cmd = new CreateCommentCommand(workItem.Id, "Hey @John.Doe check this out", null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).Publish(
            Arg.Is<Domain.Events.CommentMentionDomainEvent>(e => e.MentionedUserId == mentionedUser.Id),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyContent_Fails(string? content)
    {
        var validator = new CreateCommentValidator();
        var cmd = new CreateCommentCommand(Guid.NewGuid(), content!, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ContentTooLong_Fails()
    {
        var validator = new CreateCommentValidator();
        var cmd = new CreateCommentCommand(Guid.NewGuid(), new string('a', 10001), null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
