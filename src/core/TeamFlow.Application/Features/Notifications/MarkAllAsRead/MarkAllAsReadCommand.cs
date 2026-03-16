using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Notifications.MarkAllAsRead;

public sealed record MarkAllAsReadCommand : IRequest<Result>;
