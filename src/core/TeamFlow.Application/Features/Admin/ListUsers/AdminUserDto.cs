using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ListUsers;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string Name,
    SystemRole SystemRole,
    DateTime CreatedAt
);
