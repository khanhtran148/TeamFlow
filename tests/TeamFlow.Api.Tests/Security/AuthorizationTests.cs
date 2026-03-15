using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;

namespace TeamFlow.Api.Tests.Security;

public sealed class AuthorizationTests
{
    [Fact]
    public void ApiControllerBase_HasAuthorizeAttribute()
    {
        var attribute = typeof(ApiControllerBase)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

        attribute.Should().ContainSingle("all controllers must require authentication by default");
    }

    [Theory]
    [InlineData(typeof(Controllers.ProjectsController))]
    [InlineData(typeof(Controllers.WorkItemsController))]
    [InlineData(typeof(Controllers.ReleasesController))]
    [InlineData(typeof(Controllers.BacklogController))]
    [InlineData(typeof(Controllers.KanbanController))]
    public void AllControllers_InheritAuthorize_FromBase(Type controllerType)
    {
        var baseType = controllerType.BaseType;
        baseType.Should().Be(typeof(ApiControllerBase));

        var attribute = typeof(ApiControllerBase)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

        attribute.Should().NotBeEmpty(
            $"{controllerType.Name} inherits from ApiControllerBase which must have [Authorize]");
    }
}
