using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(
    bool? IsRead,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<NotificationDto>>>;
