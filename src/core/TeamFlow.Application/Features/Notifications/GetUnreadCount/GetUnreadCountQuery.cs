using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Notifications.GetUnreadCount;

public sealed record GetUnreadCountQuery : IRequest<Result<UnreadCountDto>>;
