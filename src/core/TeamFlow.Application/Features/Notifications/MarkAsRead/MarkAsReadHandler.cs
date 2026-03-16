using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Notifications.MarkAsRead;

public sealed class MarkAsReadHandler(
    IInAppNotificationRepository notificationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<MarkAsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken ct)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, ct);
        if (notification is null)
            return Result.Failure("Notification not found");

        if (notification.RecipientId != currentUser.Id)
            return Result.Failure("Access denied");

        await notificationRepository.MarkAsReadAsync(request.NotificationId, currentUser.Id, ct);
        return Result.Success();
    }
}
