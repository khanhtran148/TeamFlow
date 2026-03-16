using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class InvitationRepository(TeamFlowDbContext context) : IInvitationRepository
{
    public async Task<Invitation> AddAsync(Invitation invitation, CancellationToken ct = default)
    {
        context.Invitations.Add(invitation);
        await context.SaveChangesAsync(ct);
        return invitation;
    }

    public async Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await context.Invitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IEnumerable<Invitation>> ListByOrgAsync(Guid organizationId, CancellationToken ct = default)
        => await context.Invitations
            .AsNoTracking()
            .Where(i => i.OrganizationId == organizationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Invitation>> ListPendingByEmailAsync(string email, CancellationToken ct = default)
        => await context.Invitations
            .AsNoTracking()
            .Where(i => i.Email == email && i.Status == InviteStatus.Pending && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<Invitation> UpdateAsync(Invitation invitation, CancellationToken ct = default)
    {
        context.Invitations.Update(invitation);
        await context.SaveChangesAsync(ct);
        return invitation;
    }
}
