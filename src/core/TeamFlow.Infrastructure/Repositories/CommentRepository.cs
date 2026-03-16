using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class CommentRepository(TeamFlowDbContext context) : ICommentRepository
{
    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Comments
            .IgnoreQueryFilters()
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Comment> AddAsync(Comment comment, CancellationToken ct = default)
    {
        context.Comments.Add(comment);
        await context.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<Comment> UpdateAsync(Comment comment, CancellationToken ct = default)
    {
        context.Comments.Update(comment);
        await context.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<(IEnumerable<Comment> Items, int TotalCount)> GetByWorkItemPagedAsync(
        Guid workItemId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Comments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Replies.Where(r => r.DeletedAt == null))
                .ThenInclude(r => r.Author)
            .Where(c => c.WorkItemId == workItemId && c.ParentCommentId == null);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
