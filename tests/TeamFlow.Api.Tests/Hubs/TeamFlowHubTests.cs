using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Api.Tests.Hubs;

public sealed class TeamFlowHubTests
{
    private static readonly Guid UserId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ProjectId = new("00000000-0000-0000-0000-000000000099");

    private readonly IPermissionChecker _permissionChecker = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task JoinProject_WithPermission_Succeeds()
    {
        _currentUser.Id.Returns(UserId);
        _permissionChecker.HasPermissionAsync(UserId, ProjectId, Permission.Project_View, Arg.Any<CancellationToken>())
            .Returns(true);

        var hub = CreateHub();

        var act = () => hub.JoinProject(ProjectId.ToString());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task JoinProject_WithoutPermission_ThrowsHubException()
    {
        _currentUser.Id.Returns(UserId);
        _permissionChecker.HasPermissionAsync(UserId, ProjectId, Permission.Project_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var hub = CreateHub();

        var act = () => hub.JoinProject(ProjectId.ToString());

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task JoinProject_InvalidGuid_ThrowsHubException()
    {
        _currentUser.Id.Returns(UserId);

        var hub = CreateHub();

        var act = () => hub.JoinProject("not-a-guid");

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*Invalid*");
    }

    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IRetroSessionRepository _retroSessionRepo = Substitute.For<IRetroSessionRepository>();

    private Api.Hubs.TeamFlowHub CreateHub()
    {
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<Api.Hubs.TeamFlowHub>>();
        var hub = new Api.Hubs.TeamFlowHub(logger, _permissionChecker, _currentUser, _workItemRepo, _retroSessionRepo);

        var clients = Substitute.For<IHubCallerClients>();
        var groups = Substitute.For<IGroupManager>();
        var context = Substitute.For<HubCallerContext>();

        context.ConnectionId.Returns("test-connection-id");
        hub.Clients = clients;
        hub.Groups = groups;
        hub.Context = context;

        return hub;
    }
}
