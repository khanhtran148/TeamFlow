using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Features.WorkItems.CreateWorkItem;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class CreateWorkItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_Epic_CreatesWithNoParent()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateWorkItemCommand(project.Id, null, WorkItemType.Epic, "My Epic", null, Priority.High, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(WorkItemType.Epic);
        result.Value.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Epic_WithParent_ReturnsValidationError()
    {
        var project = await SeedProjectAsync();
        var parentId = Guid.NewGuid();

        var cmd = new CreateWorkItemCommand(project.Id, parentId, WorkItemType.Epic, "My Epic", null, Priority.High, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Epic cannot have a parent");
    }

    [Fact]
    public async Task Handle_Story_WithEpicParent_Succeeds()
    {
        var project = await SeedProjectAsync();
        var epic = await SeedWorkItemAsync(project.Id, b => b.AsEpic());

        var cmd = new CreateWorkItemCommand(project.Id, epic.Id, WorkItemType.UserStory, "My Story", null, Priority.Medium, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(epic.Id);
    }

    [Fact]
    public async Task Handle_Story_WithStoryParent_ReturnsValidationError()
    {
        var project = await SeedProjectAsync();
        var storyParent = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory));

        var cmd = new CreateWorkItemCommand(project.Id, storyParent.Id, WorkItemType.UserStory, "My Story", null, Priority.Medium, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("UserStory parent must be an Epic");
    }

    [Theory]
    [InlineData(WorkItemType.Task)]
    [InlineData(WorkItemType.Bug)]
    [InlineData(WorkItemType.Spike)]
    public async Task Handle_TaskBugSpike_WithStoryParent_Succeeds(WorkItemType type)
    {
        var project = await SeedProjectAsync();
        var story = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory));

        var cmd = new CreateWorkItemCommand(project.Id, story.Id, type, "My Item", null, Priority.Low, null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkItemType.Task)]
    [InlineData(WorkItemType.Bug)]
    [InlineData(WorkItemType.Spike)]
    public async Task Handle_TaskBugSpike_WithEpicParent_ReturnsError(WorkItemType type)
    {
        var project = await SeedProjectAsync();
        var epic = await SeedWorkItemAsync(project.Id, b => b.AsEpic());

        var cmd = new CreateWorkItemCommand(project.Id, epic.Id, type, "My Item", null, Priority.Low, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("must have a UserStory parent");
    }

    [Fact]
    public async Task Handle_MissingProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();

        var cmd = new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "Epic", null, Priority.High, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyTitle_ReturnsValidationError(string? title)
    {
        var validator = new CreateWorkItemValidator();
        var cmd = new CreateWorkItemCommand(Guid.NewGuid(), null, WorkItemType.Epic, title!, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}

[Collection("WorkItems")]
public sealed class CreateWorkItemPublisherTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private CapturingPublisher _publisher = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        _publisher = new CapturingPublisher();
        services.AddSingleton<MediatR.IPublisher>(_publisher);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesDomainEvent()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateWorkItemCommand(project.Id, null, WorkItemType.Epic, "My Epic", null, Priority.High, null);
        await Sender.Send(cmd);

        _publisher.HasPublished<WorkItemCreatedDomainEvent>().Should().BeTrue();
    }
}

[Collection("WorkItems")]
public sealed class CreateWorkItemPermissionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Application.Common.Interfaces.IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var project = await SeedProjectAsync();

        var cmd = new CreateWorkItemCommand(project.Id, null, WorkItemType.Epic, "Epic", null, Priority.High, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
    }
}
