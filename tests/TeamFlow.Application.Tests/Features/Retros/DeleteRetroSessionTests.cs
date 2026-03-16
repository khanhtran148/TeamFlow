using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.DeleteRetroSession;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class DeleteRetroSessionTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public DeleteRetroSessionTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private DeleteRetroSessionHandler CreateHandler() =>
        new(_retroRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidSession_DeletesSuccessfully()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .Build();

        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var command = new DeleteRetroSessionCommand(session.Id);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _retroRepo.Received(1).DeleteAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsFailure()
    {
        var sessionId = Guid.NewGuid();
        _retroRepo.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns((RetroSession?)null);

        var command = new DeleteRetroSessionCommand(sessionId);
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

        var command = new DeleteRetroSessionCommand(session.Id);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Access denied");
        await _retroRepo.DidNotReceive().DeleteAsync(Arg.Any<RetroSession>(), Arg.Any<CancellationToken>());
    }
}
