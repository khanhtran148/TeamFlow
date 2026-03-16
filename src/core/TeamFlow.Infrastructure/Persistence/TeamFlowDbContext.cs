using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence;

public class TeamFlowDbContext : DbContext
{
    public TeamFlowDbContext(DbContextOptions<TeamFlowDbContext> options) : base(options)
    {
    }

    // Core
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMembership> ProjectMemberships => Set<ProjectMembership>();

    // Auth
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Work Items
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemHistory> WorkItemHistories => Set<WorkItemHistory>();
    public DbSet<WorkItemLink> WorkItemLinks => Set<WorkItemLink>();

    // Sprint & Release
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Release> Releases => Set<Release>();

    // Retrospective
    public DbSet<RetroSession> RetroSessions => Set<RetroSession>();
    public DbSet<RetroCard> RetroCards => Set<RetroCard>();
    public DbSet<RetroVote> RetroVotes => Set<RetroVote>();
    public DbSet<RetroActionItem> RetroActionItems => Set<RetroActionItem>();

    // Comments
    public DbSet<Comment> Comments => Set<Comment>();

    // Planning Poker
    public DbSet<PlanningPokerSession> PlanningPokerSessions => Set<PlanningPokerSession>();
    public DbSet<PlanningPokerVote> PlanningPokerVotes => Set<PlanningPokerVote>();

    // Notifications
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();

    // Search
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();

    // Phase 5: Notification preferences & email outbox
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<EmailOutbox> EmailOutboxes => Set<EmailOutbox>();

    // Phase 5: Reports
    public DbSet<SprintReport> SprintReports => Set<SprintReport>();
    public DbSet<TeamHealthSummary> TeamHealthSummaries => Set<TeamHealthSummary>();

    // AI-Ready
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();
    public DbSet<SprintSnapshot> SprintSnapshots => Set<SprintSnapshot>();
    public DbSet<BurndownDataPoint> BurndownDataPoints => Set<BurndownDataPoint>();
    public DbSet<TeamVelocityHistory> TeamVelocityHistories => Set<TeamVelocityHistory>();
    public DbSet<WorkItemEmbedding> WorkItemEmbeddings => Set<WorkItemEmbedding>();
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();

    // Infrastructure
    public DbSet<JobExecutionMetric> JobExecutionMetrics => Set<JobExecutionMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamFlowDbContext).Assembly);

        // Global query filter: soft delete for WorkItems
        modelBuilder.Entity<WorkItem>()
            .HasQueryFilter(w => w.DeletedAt == null);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update UpdatedAt for BaseEntity
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && e.State is EntityState.Modified or EntityState.Added);

        foreach (var entry in entries)
        {
            ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
