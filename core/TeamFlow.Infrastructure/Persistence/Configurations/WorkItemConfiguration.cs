using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_items");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(w => w.ParentId).HasColumnName("parent_id");
        builder.Property(w => w.Type).HasColumnName("type")
            .HasConversion<string>().IsRequired();
        builder.Property(w => w.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(w => w.Description).HasColumnName("description");
        builder.Property(w => w.Status).HasColumnName("status")
            .HasConversion<string>().IsRequired();
        builder.Property(w => w.Priority).HasColumnName("priority")
            .HasConversion<string>();

        // Estimation
        builder.Property(w => w.EstimationValue).HasColumnName("estimation_value")
            .HasColumnType("decimal(6,2)");
        builder.Property(w => w.EstimationUnit).HasColumnName("estimation_unit").HasMaxLength(20);
        builder.Property(w => w.EstimationConfidence).HasColumnName("estimation_confidence");
        builder.Property(w => w.EstimationSource).HasColumnName("estimation_source").HasMaxLength(10);
        builder.Property(w => w.EstimationHistory).HasColumnName("estimation_history")
            .HasColumnType("jsonb");

        // Assignments
        builder.Property(w => w.AssigneeId).HasColumnName("assignee_id");
        builder.Property(w => w.SprintId).HasColumnName("sprint_id");
        builder.Property(w => w.ReleaseId).HasColumnName("release_id");
        builder.Property(w => w.RetroActionItemId).HasColumnName("retro_action_item_id");

        // Flexible fields
        builder.Property(w => w.CustomFields).HasColumnName("custom_fields").HasColumnType("jsonb");
        builder.Property(w => w.AiMetadata).HasColumnName("ai_metadata").HasColumnType("jsonb");
        builder.Property(w => w.ExternalRefs).HasColumnName("external_refs").HasColumnType("jsonb");

        // Timestamps
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(w => w.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(w => w.Project)
            .WithMany(p => p.WorkItems)
            .HasForeignKey(w => w.ProjectId);

        builder.HasOne(w => w.Parent)
            .WithMany(w => w.Children)
            .HasForeignKey(w => w.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Assignee)
            .WithMany(u => u.AssignedWorkItems)
            .HasForeignKey(w => w.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.Sprint)
            .WithMany(s => s.WorkItems)
            .HasForeignKey(w => w.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.Release)
            .WithMany(r => r.WorkItems)
            .HasForeignKey(w => w.ReleaseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(w => w.ProjectId).HasFilter("deleted_at IS NULL");
        builder.HasIndex(w => w.ParentId).HasFilter("deleted_at IS NULL");
        builder.HasIndex(w => w.AssigneeId);
        builder.HasIndex(w => w.SprintId);
        builder.HasIndex(w => w.ReleaseId);
    }
}
