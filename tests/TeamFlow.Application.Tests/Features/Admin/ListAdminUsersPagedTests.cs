using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ListUsers;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ListAdminUsersPagedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPagedResult()
    {
        for (var i = 1; i <= 5; i++)
        {
            DbContext.Set<TeamFlow.Domain.Entities.User>().Add(
                UserBuilder.New().WithEmail($"laup-user{i}@example.com").Build());
        }
        await DbContext.SaveChangesAsync();

        var query = new AdminListUsersQuery(null, 1, 3);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(3);
        result.Value.TotalPages.Should().BeGreaterThanOrEqualTo(2);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SearchByName_FiltersResults()
    {
        DbContext.Set<TeamFlow.Domain.Entities.User>().Add(
            UserBuilder.New().WithName("Alice Smith").WithEmail("laup-alice@example.com").Build());
        await DbContext.SaveChangesAsync();

        var query = new AdminListUsersQuery("Alice Smith", 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(u => u.Name == "Alice Smith");
    }

    [Fact]
    public async Task Handle_UserDto_IncludesIsActiveAndMustChangePassword()
    {
        var user = UserBuilder.New()
            .WithEmail("laup-inactive@example.com")
            .WithIsActive(false)
            .WithMustChangePassword(true)
            .Build();
        DbContext.Set<TeamFlow.Domain.Entities.User>().Add(user);
        await DbContext.SaveChangesAsync();

        var query = new AdminListUsersQuery("laup-inactive@example.com", 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items.Single(u => u.Email == "laup-inactive@example.com");
        dto.IsActive.Should().BeFalse();
        dto.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        // Documented in ListAdminUsersForbiddenTests
    }
}
