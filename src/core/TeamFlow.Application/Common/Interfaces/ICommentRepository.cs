using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Comment> AddAsync(Comment comment, CancellationToken ct = default);
    Task<Comment> UpdateAsync(Comment comment, CancellationToken ct = default);
    Task<(IEnumerable<Comment> Items, int TotalCount)> GetByWorkItemPagedAsync(
        Guid workItemId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
