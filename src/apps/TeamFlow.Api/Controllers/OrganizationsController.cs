using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Controllers.Base;
using TeamFlow.Application.Features.Organizations;
using TeamFlow.Application.Features.Organizations.CreateOrganization;
using TeamFlow.Application.Features.Organizations.ListOrganizations;

namespace TeamFlow.Api.Controllers;

[ApiVersion("1.0")]
public sealed class OrganizationsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationCommand cmd, CancellationToken ct)
    {
        var result = await Sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var repo = HttpContext.RequestServices.GetRequiredService<Application.Common.Interfaces.IOrganizationRepository>();
        var org = await repo.GetByIdAsync(id, ct);
        if (org is null)
            return NotFound();
        return Ok(new OrganizationDto(org.Id, org.Name, org.CreatedAt));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await Sender.Send(new ListOrganizationsQuery(), ct);
        return HandleResult(result);
    }
}
