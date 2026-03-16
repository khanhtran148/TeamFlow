using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.UpdateColumnsConfig;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class UpdateColumnsConfigTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public UpdateColumnsConfigTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UpdateColumnsConfigHandler CreateHandler() =>
        new(_retroRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidConfig_UpdatesSession()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .Build();

        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var config = JsonDocument.Parse("""[{"name":"Went Well","color":"green"},{"name":"To Improve","color":"red"}]""");
        var command = new UpdateColumnsConfigCommand(session.Id, config);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        session.ColumnsConfig.Should().BeSameAs(config);
        await _retroRepo.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _retroRepo.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns((RetroSession?)null);

        var config = JsonDocument.Parse("[]");
        var command = new UpdateColumnsConfigCommand(sessionId, config);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Retro session not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .Build();

        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Retro_Facilitate, Arg.Any<CancellationToken>())
            .Returns(false);

        var config = JsonDocument.Parse("[]");
        var command = new UpdateColumnsConfigCommand(session.Id, config);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
        await _retroRepo.DidNotReceive().UpdateAsync(Arg.Any<RetroSession>(), Arg.Any<CancellationToken>());
    }
}
