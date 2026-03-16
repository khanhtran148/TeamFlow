using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Notifications.GetNotifications;

public sealed class GetNotificationsHandler(
    IInAppNotificationRepository notificationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetNotificationsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await notificationRepository.GetByRecipientPagedAsync(
            currentUser.Id, request.IsRead, request.Page, request.PageSize, ct);

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.Type, n.Title, n.Body,
            n.ReferenceId, n.ReferenceType,
            n.IsRead, n.CreatedAt
        )).ToList();

        return Result.Success(new PagedResult<NotificationDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}
