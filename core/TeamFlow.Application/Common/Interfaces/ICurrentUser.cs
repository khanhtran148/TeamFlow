using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
    string Name { get; }
    bool IsAuthenticated { get; }
}
