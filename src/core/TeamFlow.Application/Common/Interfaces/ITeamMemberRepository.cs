using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface ITeamMemberRepository
{
    Task<IEnumerable<(Team Team, ProjectRole Role, DateTime JoinedAt)>> ListTeamsForUserAsync(
        Guid userId, CancellationToken ct = default);
}
