using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class PlanningPokerSessionConfiguration : IEntityTypeConfiguration<PlanningPokerSession>
{
    public void Configure(EntityTypeBuilder<PlanningPokerSession> builder)
    {
        builder.ToTable("planning_poker_sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.WorkItemId).HasColumnName("work_item_id").IsRequired();
        builder.Property(s => s.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(s => s.FacilitatorId).HasColumnName("facilitator_id").IsRequired();
        builder.Property(s => s.IsRevealed).HasColumnName("is_revealed").IsRequired();
        builder.Property(s => s.FinalEstimate).HasColumnName("final_estimate").HasColumnType("decimal(5,1)");
        builder.Property(s => s.ConfirmedById).HasColumnName("confirmed_by_id");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(s => s.ClosedAt).HasColumnName("closed_at").HasColumnType("timestamptz");

        builder.HasOne(s => s.WorkItem)
            .WithMany()
            .HasForeignKey(s => s.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Facilitator)
            .WithMany()
            .HasForeignKey(s => s.FacilitatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ConfirmedBy)
            .WithMany()
            .HasForeignKey(s => s.ConfirmedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.WorkItemId);

        // Unique constraint: only one active (non-closed) session per work item
        builder.HasIndex(s => s.WorkItemId)
            .HasFilter("closed_at IS NULL")
            .IsUnique()
            .HasDatabaseName("ix_planning_poker_sessions_work_item_id_active");
    }
}
