using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.ListRetroSessions;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class ListRetroSessionsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ReturnsPagedResults()
    {
        var project = await SeedProjectAsync();
        DbContext.Set<TeamFlow.Domain.Entities.RetroSession>().AddRange(
            RetroSessionBuilder.New().WithProject(project.Id).WithFacilitator(SeedUserId).Build(),
            RetroSessionBuilder.New().WithProject(project.Id).WithFacilitator(SeedUserId).Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListRetroSessionsQuery(project.Id, 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 5)]
    public async Task Handle_PassesPaginationParameters(int page, int pageSize)
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListRetroSessionsQuery(project.Id, page, pageSize));

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(page);
        result.Value.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListRetroSessionsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}

[Collection("Social")]
public sealed class ListRetroSessionsForbiddenTests(PostgresCollectionFixture fixture)
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

        var result = await Sender.Send(new ListRetroSessionsQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
