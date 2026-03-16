using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Comments.GetComments;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Comments;

[Collection("Social")]
public sealed class GetCommentsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidWorkItem_ReturnsPaginatedComments()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        DbContext.Set<TeamFlow.Domain.Entities.Comment>().AddRange(
            CommentBuilder.New().WithWorkItem(workItem.Id).WithAuthor(SeedUserId).WithContent("Comment 1").Build(),
            CommentBuilder.New().WithWorkItem(workItem.Id).WithAuthor(SeedUserId).WithContent("Comment 2").Build()
        );
        await DbContext.SaveChangesAsync();

        var query = new GetCommentsQuery(workItem.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyWorkItem_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var query = new GetCommentsQuery(workItem.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InvalidWorkItem_ReturnsNotFound()
    {
        var query = new GetCommentsQuery(Guid.NewGuid());
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Work item not found");
    }

    [Fact]
    public async Task Handle_ThreadedComments_IncludesReplies()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var parentComment = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .WithContent("Parent")
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(parentComment);
        await DbContext.SaveChangesAsync();

        var reply = CommentBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithAuthor(SeedUserId)
            .WithContent("Reply")
            .WithParent(parentComment.Id)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.Comment>().Add(reply);
        await DbContext.SaveChangesAsync();

        var query = new GetCommentsQuery(workItem.Id);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Replies.Should().HaveCount(1);
        result.Value.Items[0].Replies[0].Content.Should().Be("Reply");
    }
}

[Collection("Social")]
public sealed class GetCommentsForbiddenTests(PostgresCollectionFixture fixture)
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

        var query = new GetCommentsQuery(workItem.Id);
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
