using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Notifications.MarkAsRead;

public sealed record MarkAsReadCommand(Guid NotificationId) : IRequest<Result>;
