using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IInvitationRepository
{
    Task<Invitation> AddAsync(Invitation invitation, CancellationToken ct = default);
    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Invitation>> ListByOrgAsync(Guid organizationId, CancellationToken ct = default);
    Task<IEnumerable<Invitation>> ListPendingByEmailAsync(string email, CancellationToken ct = default);
    Task<Invitation> UpdateAsync(Invitation invitation, CancellationToken ct = default);
    Task RevokePendingByOrgAsync(Guid organizationId, CancellationToken ct = default);
}
