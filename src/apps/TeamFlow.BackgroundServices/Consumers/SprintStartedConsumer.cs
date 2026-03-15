using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.EventDriven.Consumers;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Consumers;

/// <summary>
/// Handles SprintStartedDomainEvent: creates OnStart snapshot,
/// initializes burndown data point, and broadcasts sprint.started.
/// </summary>
public sealed class SprintStartedConsumer(
    ILogger<SprintStartedConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseConsumer<SprintStartedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<SprintStartedDomainEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        // 1. Create OnStart snapshot
        var sprint = await DbContext.Sprints
            .Include(s => s.WorkItems)
            .FirstOrDefaultAsync(s => s.Id == @event.SprintId, ct);

        var totalPoints = sprint?.WorkItems
            .Sum(w => (int)(w.EstimationValue ?? 0)) ?? 0;

        var snapshotPayload = new
        {
            sprintId = @event.SprintId,
            totalPoints,
            itemCount = sprint?.WorkItems.Count ?? 0,
            startDate = @event.StartDate.ToString("yyyy-MM-dd"),
            endDate = @event.EndDate.ToString("yyyy-MM-dd")
        };

        var snapshot = new SprintSnapshot
        {
            SprintId = @event.SprintId,
            SnapshotType = "OnStart",
            IsFinal = false,
            Payload = JsonDocument.Parse(JsonSerializer.Serialize(snapshotPayload))
        };

        DbContext.SprintSnapshots.Add(snapshot);

        // 2. Initialize BurndownDataPoint for today (day 0)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataPoint = new BurndownDataPoint
        {
            SprintId = @event.SprintId,
            RecordedDate = today,
            RemainingPoints = totalPoints,
            CompletedPoints = 0,
            AddedPoints = 0,
            IsWeekend = today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
        };

        DbContext.BurndownDataPoints.Add(dataPoint);
        await DbContext.SaveChangesAsync(ct);

        // 3. Broadcast sprint.started
        await BroadcastService.BroadcastToProjectAsync(
            @event.ProjectId,
            "sprint.started",
            @event,
            ct);

        logger.LogInformation(
            "SprintStartedConsumer: Processed sprint {SprintId} — snapshot created, burndown initialized with {TotalPoints} points",
            @event.SprintId, totalPoints);
    }
}
