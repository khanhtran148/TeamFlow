using MassTransit;
using Quartz;
using TeamFlow.Application;
using TeamFlow.BackgroundServices.Consumers;
using TeamFlow.BackgroundServices.Scheduled.Jobs;
using TeamFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// ─── Layers ───────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── MassTransit + RabbitMQ ────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SprintStartedConsumer>();
    x.AddConsumer<SprintCompletedConsumer>();
    x.AddConsumer<SignalRBroadcastConsumer>();
    x.AddConsumer<SignalRWorkItemStatusBroadcastConsumer>();
    x.AddConsumer<DomainEventStoreConsumer>();

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

        // Main exchange
        cfg.Message<object>(e => e.SetEntityName("teamflow.events"));

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
        .WithCronSchedule("59 23 * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(10));

    // ── ReleaseOverdueDetectorJob: 00:05 AM daily ──
    var releaseOverdueJobKey = new JobKey("ReleaseOverdueDetectorJob");
    q.AddJob<ReleaseOverdueDetectorJob>(opts => opts
        .WithIdentity(releaseOverdueJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(releaseOverdueJobKey)
        .WithIdentity("ReleaseOverdueDetectorJob-trigger")
        .WithCronSchedule("5 0 * * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(10));

    // ── StaleItemDetectorJob: 08:00 AM daily ──
    var staleItemJobKey = new JobKey("StaleItemDetectorJob");
    q.AddJob<StaleItemDetectorJob>(opts => opts
        .WithIdentity(staleItemJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(staleItemJobKey)
        .WithIdentity("StaleItemDetectorJob-trigger")
        .WithCronSchedule("0 8 * * ?", x => x.WithMisfireHandlingInstructionDoNothing())
        .WithPriority(5));

    // ── EventPartitionCreatorJob: 03:00 AM on 25th of month ──
    var partitionJobKey = new JobKey("EventPartitionCreatorJob");
    q.AddJob<EventPartitionCreatorJob>(opts => opts
        .WithIdentity(partitionJobKey)
        .StoreDurably());
    q.AddTrigger(opts => opts
        .ForJob(partitionJobKey)
        .WithIdentity("EventPartitionCreatorJob-trigger")
        .WithCronSchedule("0 3 25 * ?", x => x.WithMisfireHandlingInstructionFireAndProceed())
        .WithPriority(15));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
    options.AwaitApplicationStarted = true;
});

var host = builder.Build();
host.Run();
