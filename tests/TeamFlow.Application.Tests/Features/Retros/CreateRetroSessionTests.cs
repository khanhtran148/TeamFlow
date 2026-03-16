using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.CreateRetroSession;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class CreateRetroSessionTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public CreateRetroSessionTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _retroRepo.AddAsync(Arg.Any<RetroSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroSession>());
    }

    private CreateRetroSessionHandler CreateHandler() =>
        new(_retroRepo, _projectRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_CreatesSession()
    {
        _projectRepo.ExistsAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateRetroSessionCommand(ProjectId, null, null, "Public");
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(ProjectId);
        result.Value.FacilitatorId.Should().Be(UserId);
        result.Value.AnonymityMode.Should().Be("Public");
    }

    [Fact]
    public async Task Handle_InvalidProject_ReturnsNotFound()
    {
        _projectRepo.ExistsAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateRetroSessionCommand(ProjectId, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Retro_Facilitate, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new CreateRetroSessionCommand(ProjectId, null, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    public async Task Validate_InvalidAnonymityMode_Fails(string mode)
    {
        var validator = new CreateRetroSessionValidator();
        var cmd = new CreateRetroSessionCommand(ProjectId, null, null, mode);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Public")]
    [InlineData("Anonymous")]
    public async Task Validate_ValidAnonymityMode_Passes(string mode)
    {
        var validator = new CreateRetroSessionValidator();
        var cmd = new CreateRetroSessionCommand(ProjectId, null, null, mode);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
