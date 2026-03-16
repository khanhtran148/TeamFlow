using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.UpdateRelease;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class UpdateReleaseTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).WithName("old").Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateReleaseCommand(release.Id, "v2.0.0", "Updated", null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("v2.0.0");
    }

    [Fact]
    public async Task Handle_NotesLocked_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).WithNotesLocked(true).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateReleaseCommand(release.Id, "v2.0.0", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_NonExistentRelease_ReturnsNotFound()
    {
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), "v2.0.0", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Release not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_Fails(string? name)
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), name!, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_EmptyReleaseId_Fails()
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.Empty, "Valid Name", null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_NameTooLong_Fails()
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), new string('A', 101), null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ValidCommand_Passes()
    {
        var validator = new UpdateReleaseValidator();
        var cmd = new UpdateReleaseCommand(Guid.NewGuid(), "Valid Name", "Desc", null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Releases")]
public sealed class UpdateReleaseDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateReleaseCommand(release.Id, "v2.0.0", null, null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
