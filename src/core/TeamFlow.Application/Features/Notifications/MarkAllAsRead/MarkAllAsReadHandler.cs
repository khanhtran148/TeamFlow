using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Notifications.MarkAllAsRead;

public sealed class MarkAllAsReadHandler(
    IInAppNotificationRepository notificationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<MarkAllAsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllAsReadCommand request, CancellationToken ct)
    {
        await notificationRepository.MarkAllAsReadAsync(currentUser.Id, ct);
        return Result.Success();
    }
}
