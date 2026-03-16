using FluentAssertions;
using TeamFlow.Application.Features.Comments.UpdateComment;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

[Collection("Social")]
public sealed class UpdateCommentTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_OwnComment_UpdatesSuccessfully()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .WithContent("Original")
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(comment);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateCommentCommand(comment.Id, "Updated content");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Updated content");
        result.Value.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NotOwnComment_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var otherUser = UserBuilder.New().WithEmail("update-comment-other@example.com").Build();
        DbContext.Users.Add(otherUser);
        await DbContext.SaveChangesAsync();

        var comment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(otherUser.Id)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(comment);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateCommentCommand(comment.Id, "Trying to edit");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_DeletedComment_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var comment = CommentBuilder.New().WithWorkItem(workItem.Id).WithAuthor(SeedUserId).Deleted().Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(comment);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateCommentCommand(comment.Id, "Updated");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Comment not found");
    }

    [Fact]
    public async Task Handle_NonExistentComment_ReturnsNotFound()
    {
        var cmd = new UpdateCommentCommand(Guid.NewGuid(), "Updated");
        var result = await Sender.Send(cmd);

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
