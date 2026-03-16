using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Notifications.GetUnreadCount;

public sealed class GetUnreadCountHandler(
    IInAppNotificationRepository notificationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountDto>>
{
    public async Task<Result<UnreadCountDto>> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        var count = await notificationRepository.GetUnreadCountAsync(currentUser.Id, ct);
        return Result.Success(new UnreadCountDto(count));
    }
}
