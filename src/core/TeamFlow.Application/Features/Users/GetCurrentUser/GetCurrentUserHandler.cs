using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Users.GetCurrentUser;

public sealed class GetCurrentUserHandler(
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        if (user is null)
            return Result.Failure<CurrentUserDto>("User not found");

        var orgs = await organizationRepository.ListByUserAsync(currentUser.Id, ct);

        var orgDtos = orgs
            .Select(o => new UserOrganizationDto(o.Id, o.Name))
            .ToList();

        return Result.Success(new CurrentUserDto(
            user.Id,
            user.Email,
            user.Name,
            orgDtos
        ));
    }
}
