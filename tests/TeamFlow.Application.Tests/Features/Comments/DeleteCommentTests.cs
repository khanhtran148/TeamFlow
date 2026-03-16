using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Comments.DeleteComment;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

[Collection("Social")]
public sealed class DeleteCommentTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_OwnComment_SoftDeletes()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(comment);
        await DbContext.SaveChangesAsync();

        var cmd = new DeleteCommentCommand(comment.Id);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var persisted = await DbContext.Set<TeamFlow.Domain.Entities.Comment>()
            .IgnoreQueryFilters()
            .SingleAsync(c => c.Id == comment.Id);
        persisted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NotOwnComment_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var otherUser = UserBuilder.New().WithEmail("delete-comment-other@example.com").Build();
        DbContext.Users.Add(otherUser);
        await DbContext.SaveChangesAsync();

        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(otherUser.Id)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(comment);
        await DbContext.SaveChangesAsync();

        var cmd = new DeleteCommentCommand(comment.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_AlreadyDeleted_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var comment = CommentBuilder.New().WithWorkItem(workItem.Id).WithAuthor(SeedUserId).Deleted().Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(comment);
        await DbContext.SaveChangesAsync();

        var cmd = new DeleteCommentCommand(comment.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }

    [Fact]
    public async Task Handle_NonExistentComment_ReturnsNotFound()
    {
        var cmd = new DeleteCommentCommand(Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }
}
