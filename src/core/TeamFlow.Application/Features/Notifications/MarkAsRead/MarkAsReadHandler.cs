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
        await notificationRepository.MarkAsReadAsync(request.NotificationId, currentUser.Id, ct);
        return Result.Success();
    }
}
