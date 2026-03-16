using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Comments.GetComments;

public sealed record GetCommentsQuery(
    Guid WorkItemId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<GetCommentsResponse>>;

public sealed record GetCommentsResponse(
    IReadOnlyList<CommentDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
