using MassTransit;
using Quartz;
using TeamFlow.Application;
using TeamFlow.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// ─── Layers ───────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── MassTransit + RabbitMQ ────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Phase 0: No consumers registered yet
    // Phase 1+: Consumers registered here
    // x.AddConsumer<WorkItemStatusChangedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rabbitMqSection = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(
            rabbitMqSection["Host"] ?? "localhost",
            ushort.Parse(rabbitMqSection["Port"] ?? "5672"),
            rabbitMqSection["VirtualHost"] ?? "/",
            h =>
            {
                h.Username(rabbitMqSection["Username"] ?? "teamflow");
                h.Password(rabbitMqSection["Password"] ?? "teamflow_dev");
            });

        // Main exchange
        cfg.Message<object>(e => e.SetEntityName("teamflow.events"));

        // Retry policy: immediate → 30s → 5min → 30min → DLQ
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

    // Phase 0: No jobs scheduled yet
    // Phase 1+: Jobs registered here
    // Example:
    // var burndownJobKey = new JobKey("BurndownSnapshotJob");
    // q.AddJob<BurndownSnapshotJob>(opts => opts.WithIdentity(burndownJobKey));
    // q.AddTrigger(opts => opts
    //     .ForJob(burndownJobKey)
    //     .WithCronSchedule("59 23 * * ?")
    //     .WithIdentity("BurndownSnapshotJob-trigger"));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
    options.AwaitApplicationStarted = true;
});

var host = builder.Build();
host.Run();
