using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Organization> AddAsync(Organization organization, CancellationToken ct = default);
    Task<IEnumerable<Organization>> ListByUserAsync(Guid userId, CancellationToken ct = default);
}
