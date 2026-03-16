using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<User>> GetByDisplayNamesAsync(IEnumerable<string> displayNames, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<IEnumerable<User>> ListAllAsync(CancellationToken ct = default);
    Task<(IEnumerable<User> Items, int TotalCount)> ListPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);
}
