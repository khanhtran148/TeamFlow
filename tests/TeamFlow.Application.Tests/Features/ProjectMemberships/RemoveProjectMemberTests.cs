using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships.RemoveProjectMember;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.ProjectMemberships;

[Collection("Projects")]
public sealed class RemoveProjectMemberTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingMembership_DeletesSuccessfully()
    {
        var project = await SeedProjectAsync();
        var membership = ProjectMembershipBuilder.New()
            .WithProject(project.Id)
            .WithMember(Guid.NewGuid())
            .WithRole(ProjectRole.Developer)
            .Build();
        DbContext.ProjectMemberships.Add(membership);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RemoveProjectMemberCommand(membership.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.ProjectMemberships.FindAsync(membership.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MembershipNotFound_ReturnsFailure()
    {
        var result = await Sender.Send(new RemoveProjectMemberCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("Projects")]
public sealed class RemoveProjectMemberForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var membership = ProjectMembershipBuilder.New()
            .WithProject(project.Id)
            .WithMember(Guid.NewGuid())
            .WithRole(ProjectRole.Developer)
            .Build();
        DbContext.ProjectMemberships.Add(membership);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RemoveProjectMemberCommand(membership.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
