using FluentAssertions;
using TeamFlow.Application.Features.Projects.ListProjects;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Projects;

[Collection("Projects")]
public sealed class ListProjectsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<Project> SeedProjectWithMembershipAsync(string? name = null)
    {
        var project = await SeedProjectAsync(b =>
        {
            if (name is not null) b.WithName(name);
        });
        DbContext.ProjectMemberships.Add(
            ProjectMembershipBuilder.New()
                .WithProject(project.Id)
                .WithMember(SeedUserId)
                .Build());
        await DbContext.SaveChangesAsync();
        return project;
    }

    [Fact]
    public async Task Handle_WithProjects_ReturnsPagedResult()
    {
        await SeedProjectWithMembershipAsync("Project A");
        await SeedProjectWithMembershipAsync("Project B");

        var query = new ListProjectsQuery(null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_FilterByStatus_ReturnsOnlyMatchingProjects()
    {
        await SeedProjectWithMembershipAsync("Active Project");
        var archivedProject = await SeedProjectWithMembershipAsync("Archived Project");
        archivedProject.Status = "Archived";
        await DbContext.SaveChangesAsync();

        var query = new ListProjectsQuery(null, "Active", null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(p => p.Status == "Active");
    }

    [Fact]
    public async Task Handle_SearchByName_ReturnsMatchingProject()
    {
        var uniqueName = "UniqueName_" + Guid.NewGuid().ToString("N")[..8];
        await SeedProjectWithMembershipAsync(uniqueName);

        var query = new ListProjectsQuery(null, null, uniqueName, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(p => p.Name == uniqueName);
    }
}
