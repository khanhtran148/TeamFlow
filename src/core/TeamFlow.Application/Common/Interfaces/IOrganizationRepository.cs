using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);
    Task<Organization> AddAsync(Organization organization, CancellationToken ct = default);
    Task<Organization> UpdateAsync(Organization organization, CancellationToken ct = default);
    Task<IEnumerable<Organization>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Organization>> ListAllAsync(CancellationToken ct = default);
}
