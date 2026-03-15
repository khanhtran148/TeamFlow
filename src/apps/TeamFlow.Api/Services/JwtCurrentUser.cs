using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Api.Services;

public sealed class JwtCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid Id
    {
        get
        {
            var sub = User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Email
        => User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? User?.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;

    public string Name
        => User?.FindFirst("name")?.Value
            ?? User?.FindFirst(ClaimTypes.Name)?.Value
            ?? string.Empty;

    public bool IsAuthenticated
        => User?.Identity?.IsAuthenticated ?? false;
}
