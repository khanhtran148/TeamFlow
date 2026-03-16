using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IEmailOutboxRepository
{
    Task AddAsync(EmailOutbox entry, CancellationToken ct = default);
    Task<IReadOnlyList<EmailOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task UpdateAsync(EmailOutbox entry, CancellationToken ct = default);
    Task<IReadOnlyList<EmailOutbox>> GetDeadLetteredAsync(int page, int pageSize, CancellationToken ct = default);
}
