using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Search.DeleteSavedFilter;

public sealed class DeleteSavedFilterHandler(
    ISavedFilterRepository savedFilterRepository,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteSavedFilterCommand, Result>
{
    public async Task<Result> Handle(DeleteSavedFilterCommand request, CancellationToken ct)
    {
        var filter = await savedFilterRepository.GetByIdAsync(request.FilterId, ct);
        if (filter is null)
            return Result.Failure("Saved filter not found");

        if (filter.UserId != currentUser.Id)
            return Result.Failure("Access denied");

        await savedFilterRepository.DeleteAsync(request.FilterId, ct);
        return Result.Success();
    }
}
