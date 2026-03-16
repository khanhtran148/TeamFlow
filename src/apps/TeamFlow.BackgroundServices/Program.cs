using MassTransit;
using Quartz;
using TeamFlow.Application;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.Consumers;
using TeamFlow.BackgroundServices.Scheduled.Jobs;
using TeamFlow.BackgroundServices.Services;
using TeamFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// ─── Layers ───────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();
builder.Services.AddSingleton<IBroadcastService, NoOpBroadcastService>();

// ─── MassTransit + RabbitMQ ────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SprintStartedConsumer>();
    x.AddConsumer<SprintCompletedConsumer>();
    x.AddConsumer<SignalRBroadcastConsumer>();
    x.AddConsumer<SignalRWorkItemStatusBroadcastConsumer>();
    x.AddConsumer<DomainEventStoreConsumer>();
    x.AddConsumer<WorkItemAssignedNotificationConsumer>();
    x.AddConsumer<NotificationCreatedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rabbitMqSection = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(
            rabbitMqSection["Host"] ?? "localhost",
            ushort.Parse(rabbitMqSection["Port"] ?? "5672"),
            rabbitMqSection["VirtualHost"] ?? "/",
            h =>
            {
                h.Username(rabbitMqSection["Username"]
                    ?? throw new InvalidOperationException("RabbitMQ:Username must be configured"));
                h.Password(rabbitMqSection["Password"]
                    ?? throw new InvalidOperationException("RabbitMQ:Password must be configured"));
            });

        // Retry policy: immediate -> 30s -> 5min -> 30min -> DLQ
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30)
        ));

        cfg.ConfigureEndpoints(ctx);
    });
});

// ─── Quartz.NET ────────────────────────────────────────────────────────────
builder.Services.AddQuartz(q =>
{
    q.UsePersistentStore(store =>
    {
        store.UsePostgres(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection"));
        store.UseJsonSerializer();
        store.UseClustering();
    });

    // ── BurndownSnapshotJob: 11:59 PM daily ──
    var burndownJobKey = new JobKey("BurndownSnapshotJob");
    q.AddJob<BurndownSnapshotJob>(opts => opts
        .WithIdentity(burndownJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(burndownJobKey)
        .WithIdentity("BurndownSnapshotJob-trigger")
        .WithCronSchedule("0 59 23 * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(10));

    // ── ReleaseOverdueDetectorJob: 00:05 AM daily ──
    var releaseOverdueJobKey = new JobKey("ReleaseOverdueDetectorJob");
    q.AddJob<ReleaseOverdueDetectorJob>(opts => opts
        .WithIdentity(releaseOverdueJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(releaseOverdueJobKey)
        .WithIdentity("ReleaseOverdueDetectorJob-trigger")
        .WithCronSchedule("0 5 0 * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(10));

    // ── StaleItemDetectorJob: 08:00 AM daily ──
    var staleItemJobKey = new JobKey("StaleItemDetectorJob");
    q.AddJob<StaleItemDetectorJob>(opts => opts
        .WithIdentity(staleItemJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(staleItemJobKey)
        .WithIdentity("StaleItemDetectorJob-trigger")
        .WithCronSchedule("0 0 8 * * ?", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(5));

    // ── EventPartitionCreatorJob: 03:00 AM on 25th of month ──
    var partitionJobKey = new JobKey("EventPartitionCreatorJob");
    q.AddJob<EventPartitionCreatorJob>(opts => opts
        .WithIdentity(partitionJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(partitionJobKey)
        .WithIdentity("EventPartitionCreatorJob-trigger")
        .WithCronSchedule("0 0 3 25 * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(15));

    // ── Phase 5: EmailOutboxProcessorJob: every 30 seconds ──
    var emailOutboxJobKey = new JobKey("EmailOutboxProcessorJob");
    q.AddJob<EmailOutboxProcessorJob>(opts => opts
        .WithIdentity(emailOutboxJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(emailOutboxJobKey)
        .WithIdentity("EmailOutboxProcessorJob-trigger")
        .WithCronSchedule("0/30 * * * * ?", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(10));

    // ── Phase 5: DeadlineReminderJob: 08:00 AM daily ──
    var deadlineReminderJobKey = new JobKey("DeadlineReminderJob");
    q.AddJob<DeadlineReminderJob>(opts => opts
        .WithIdentity(deadlineReminderJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(deadlineReminderJobKey)
        .WithIdentity("DeadlineReminderJob-trigger")
        .WithCronSchedule("0 0 8 * * ?", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(5));

    // ── Phase 5: VelocityAggregatorJob: Monday 07:00 AM ──
    var velocityAggregatorJobKey = new JobKey("VelocityAggregatorJob");
    q.AddJob<VelocityAggregatorJob>(opts => opts
        .WithIdentity(velocityAggregatorJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(velocityAggregatorJobKey)
        .WithIdentity("VelocityAggregatorJob-trigger")
        .WithCronSchedule("0 0 7 ? * MON", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(5));

    // ── Phase 5: SprintReportGeneratorJob: on-demand (finds unreported sprints) ──
    var sprintReportJobKey = new JobKey("SprintReportGeneratorJob");
    q.AddJob<SprintReportGeneratorJob>(opts => opts
        .WithIdentity(sprintReportJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(sprintReportJobKey)
        .WithIdentity("SprintReportGeneratorJob-trigger")
        .WithCronSchedule("0 0 0 * * ?", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(5));

    // ── Phase 5: DataArchivalJob: 03:00 AM on 1st of month ──
    var dataArchivalJobKey = new JobKey("DataArchivalJob");
    q.AddJob<DataArchivalJob>(opts => opts
        .WithIdentity(dataArchivalJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(dataArchivalJobKey)
        .WithIdentity("DataArchivalJob-trigger")
        .WithCronSchedule("0 0 3 1 * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(5));

    // ── Phase 5: TeamHealthSummaryJob: Monday 07:30 AM (after velocity) ──
    var teamHealthJobKey = new JobKey("TeamHealthSummaryJob");
    q.AddJob<TeamHealthSummaryJob>(opts => opts
        .WithIdentity(teamHealthJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(teamHealthJobKey)
        .WithIdentity("TeamHealthSummaryJob-trigger")
        .WithCronSchedule("0 30 7 ? * MON", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(5));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
    options.AwaitApplicationStarted = true;
});

var host = builder.Build();
host.Run();
