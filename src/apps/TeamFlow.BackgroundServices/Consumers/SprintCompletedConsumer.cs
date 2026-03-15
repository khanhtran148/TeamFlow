using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.EventDriven.Consumers;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Consumers;

/// <summary>
/// Handles SprintCompletedDomainEvent: creates final snapshot,
/// records velocity, and broadcasts sprint.completed.
/// </summary>
public sealed class SprintCompletedConsumer(
    ILogger<SprintCompletedConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseConsumer<SprintCompletedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<SprintCompletedDomainEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        // 1. Create OnClose snapshot (is_final: true)
        var snapshotPayload = new
        {
            sprintId = @event.SprintId,
            plannedPoints = @event.PlannedPoints,
            completedPoints = @event.CompletedPoints,
            completionRate = @event.PlannedPoints > 0
                ? Math.Round((double)@event.CompletedPoints / @event.PlannedPoints * 100, 1)
                : 0
        };

        var snapshot = new SprintSnapshot
        {
            SprintId = @event.SprintId,
            SnapshotType = "OnClose",
            IsFinal = true,
            Payload = JsonDocument.Parse(JsonSerializer.Serialize(snapshotPayload))
        };

        DbContext.SprintSnapshots.Add(snapshot);

        // 2. Record velocity in TeamVelocityHistory
        var velocityRecord = new TeamVelocityHistory
        {
            ProjectId = @event.ProjectId,
            SprintId = @event.SprintId,
            PlannedPoints = @event.PlannedPoints,
            CompletedPoints = @event.CompletedPoints,
            Velocity = @event.CompletedPoints
        };

        DbContext.TeamVelocityHistories.Add(velocityRecord);
        await DbContext.SaveChangesAsync(ct);

        // 3. Broadcast sprint.completed
        await BroadcastService.BroadcastToProjectAsync(
            @event.ProjectId,
            "sprint.completed",
            @event,
            ct);

        logger.LogInformation(
            "SprintCompletedConsumer: Processed sprint {SprintId} — final snapshot created, velocity {Velocity} recorded",
            @event.SprintId, @event.CompletedPoints);
    }
}
