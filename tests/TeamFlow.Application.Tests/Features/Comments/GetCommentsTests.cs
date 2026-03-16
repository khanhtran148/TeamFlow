using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Comments.GetComments;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

public sealed class GetCommentsTests
{
    private readonly ICommentRepository _commentRepo = Substitute.For<ICommentRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetCommentsTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetCommentsHandler CreateHandler() =>
        new(_commentRepo, _workItemRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidWorkItem_ReturnsPaginatedComments()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var author = new User { Name = "Author" };
        var comments = new List<Comment>
        {
            new() { WorkItemId = workItem.Id, AuthorId = UserId, Content = "Comment 1", Author = author, Replies = [] },
            new() { WorkItemId = workItem.Id, AuthorId = UserId, Content = "Comment 2", Author = author, Replies = [] }
        };
        _commentRepo.GetByWorkItemPagedAsync(workItem.Id, 1, 20, Arg.Any<CancellationToken>())
            .Returns((comments.AsEnumerable(), 2));

        var query = new GetCommentsQuery(workItem.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyWorkItem_ReturnsEmptyList()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        _commentRepo.GetByWorkItemPagedAsync(workItem.Id, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<Comment>(), 0));

        var query = new GetCommentsQuery(workItem.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InvalidWorkItem_ReturnsNotFound()
    {
        _workItemRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkItem?)null);

        var query = new GetCommentsQuery(Guid.NewGuid());
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Work item not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Comment_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var query = new GetCommentsQuery(workItem.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_ThreadedComments_IncludesReplies()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var author = new User { Name = "Author" };
        var reply = new Comment
        {
            WorkItemId = workItem.Id,
            AuthorId = UserId,
            Content = "Reply",
            Author = author,
            Replies = []
        };
        var parentComment = new Comment
        {
            WorkItemId = workItem.Id,
            AuthorId = UserId,
            Content = "Parent",
            Author = author,
            Replies = [reply]
        };

        _commentRepo.GetByWorkItemPagedAsync(workItem.Id, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { parentComment }.AsEnumerable(), 1));

        var query = new GetCommentsQuery(workItem.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Replies.Should().HaveCount(1);
        result.Value.Items[0].Replies[0].Content.Should().Be("Reply");
    }
}
