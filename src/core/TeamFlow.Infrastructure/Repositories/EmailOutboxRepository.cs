using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class EmailOutboxRepository(TeamFlowDbContext context) : IEmailOutboxRepository
{
    public async Task AddAsync(EmailOutbox entry, CancellationToken ct = default)
    {
        context.EmailOutboxes.Add(entry);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EmailOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default)
        => await context.EmailOutboxes
            .Where(e => (e.Status == EmailStatus.Pending || e.Status == EmailStatus.Failed)
                && (e.NextRetryAt == null || e.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task UpdateAsync(EmailOutbox entry, CancellationToken ct = default)
    {
        context.EmailOutboxes.Update(entry);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EmailOutbox>> GetDeadLetteredAsync(
        int page, int pageSize, CancellationToken ct = default)
        => await context.EmailOutboxes
            .AsNoTracking()
            .Where(e => e.Status == EmailStatus.DeadLettered)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}
