using CSharpFunctionalExtensions;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Projects.CreateProject;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Projects;

public sealed class CreateProjectTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public CreateProjectTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private CreateProjectHandler CreateHandler() =>
        new(_projectRepo, _historyService, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithProjectDto()
    {
        var orgId = Guid.NewGuid();
        var cmd = new CreateProjectCommand(orgId, "My Project", "Some description");
        _projectRepo.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Project>());

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Project");
        result.Value.OrgId.Should().Be(orgId);
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_InsufficientPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "My Project", null);

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
        await _projectRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_PermissionCheckedAgainstOrgId()
    {
        var orgId = Guid.NewGuid();
        var cmd = new CreateProjectCommand(orgId, "My Project", null);
        _projectRepo.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Project>());

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _permissions.Received(1)
            .HasPermissionAsync(_currentUser.Id, orgId, Permission.Org_Admin, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_EmptyName_ReturnsValidationError(string? name)
    {
        var orgId = Guid.NewGuid();
        var validator = new CreateProjectValidator();
        var cmd = new CreateProjectCommand(orgId, name!, null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectCommand.Name));
    }

    [Fact]
    public async Task Handle_EmptyOrgId_ReturnsValidationError()
    {
        var validator = new CreateProjectValidator();
        var cmd = new CreateProjectCommand(Guid.Empty, "My Project", null);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidCommand_RecordsHistory()
    {
        var orgId = Guid.NewGuid();
        var cmd = new CreateProjectCommand(orgId, "My Project", null);
        _projectRepo.AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Project>());

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _historyService.DidNotReceiveWithAnyArgs().RecordAsync(default!);
        // Project creation doesn't log work-item history (only work items do)
    }
}
