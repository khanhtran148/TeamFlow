using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Organizations;
using TeamFlow.Application.Features.Organizations.CreateOrganization;
using TeamFlow.Application.Features.Organizations.GetById;
using TeamFlow.Application.Features.Organizations.GetBySlug;
using TeamFlow.Application.Features.Organizations.ListMyOrganizations;
using TeamFlow.Application.Features.Organizations.ListOrganizations;
using TeamFlow.Application.Features.Organizations.UpdateOrganization;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class OrganizationsController : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        var cmd = new UpdateOrganizationCommand(id, request.Name, request.Slug);
        var result = await Sender.Send(cmd, ct);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await Sender.Send(new GetOrganizationByIdQuery(id), ct);
        return HandleResult(result);
    }

    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await Sender.Send(new GetOrganizationBySlugQuery(slug), ct);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await Sender.Send(new ListOrganizationsQuery(), ct);
        return HandleResult(result);
    }
}

/// <summary>Route for ME/organizations — separate controller to match /me/organizations route.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Microsoft.AspNetCore.Authorization.Authorize]
public sealed class MeController : ApiControllerBase
{
    [HttpGet("organizations")]
    [ProducesResponseType(typeof(IEnumerable<MyOrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyOrganizations(CancellationToken ct)
    {
        var result = await Sender.Send(new ListMyOrganizationsQuery(), ct);
        return HandleResult(result);
    }
}

public sealed record UpdateOrganizationRequest(string Name, string Slug);
