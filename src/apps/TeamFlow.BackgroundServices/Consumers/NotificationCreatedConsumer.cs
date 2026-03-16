using MassTransit;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.EventDriven.Consumers;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Consumers;

public sealed class NotificationCreatedConsumer(
    ILogger<NotificationCreatedConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseConsumer<NotificationCreatedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<NotificationCreatedDomainEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        await BroadcastService.BroadcastToUserAsync(
            @event.RecipientId,
            "notification.created",
            new { @event.NotificationId, @event.Type, @event.Title },
            ct);

        logger.LogInformation(
            "NotificationCreatedConsumer: Broadcast notification.created to user {UserId}",
            @event.RecipientId);
    }
}
