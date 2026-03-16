using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Comments.CreateComment;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

[Collection("Social")]
public sealed class CreateCommentTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesComment()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var cmd = new CreateCommentCommand(workItem.Id, "Great work!", null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Great work!");
        result.Value.AuthorId.Should().Be(SeedUserId);
        result.Value.WorkItemId.Should().Be(workItem.Id);
    }

    [Fact]
    public async Task Handle_InvalidWorkItem_ReturnsNotFound()
    {
        var cmd = new CreateCommentCommand(Guid.NewGuid(), "Comment", null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Work item not found");
    }

    [Fact]
    public async Task Handle_ParentNotFound_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var cmd = new CreateCommentCommand(workItem.Id, "Reply", Guid.NewGuid());
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Parent comment not found");
    }

    [Fact]
    public async Task Handle_NestedReply_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        // Seed a top-level grandparent comment first, then the parent referencing it
        var grandParentComment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(grandParentComment);
        await DbContext.SaveChangesAsync();

        // parentComment is itself a reply (has a parent), making any reply to it a nested reply
        var parentComment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .WithParent(grandParentComment.Id)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(parentComment);
        await DbContext.SaveChangesAsync();

        var cmd = new CreateCommentCommand(workItem.Id, "Nested reply", parentComment.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Nested replies are not allowed");
    }

    [Fact]
    public async Task Handle_DeletedParent_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var deletedParent = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .Deleted()
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(deletedParent);
        await DbContext.SaveChangesAsync();

        var cmd = new CreateCommentCommand(workItem.Id, "Reply to deleted", deletedParent.Id);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Parent comment not found");
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

[Collection("Social")]
public sealed class CreateCommentForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var cmd = new CreateCommentCommand(workItem.Id, "Comment", null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
