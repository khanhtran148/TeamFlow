using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class WorkItemHistoryConfiguration : IEntityTypeConfiguration<WorkItemHistory>
{
    public void Configure(EntityTypeBuilder<WorkItemHistory> builder)
    {
        builder.ToTable("work_item_histories");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.WorkItemId).HasColumnName("work_item_id").IsRequired();
        builder.Property(h => h.ActorId).HasColumnName("actor_id");
        builder.Property(h => h.ActorType).HasColumnName("actor_type").HasMaxLength(10).IsRequired();
        builder.Property(h => h.ActionType).HasColumnName("action_type").HasMaxLength(50).IsRequired();
        builder.Property(h => h.FieldName).HasColumnName("field_name").HasMaxLength(100);
        builder.Property(h => h.OldValue).HasColumnName("old_value");
        builder.Property(h => h.NewValue).HasColumnName("new_value");
        builder.Property(h => h.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        // Append-only: no cascade delete, no EF delete behavior.
        // Navigation is optional because WorkItem has a soft-delete query filter;
        // history rows must remain visible even when the parent WorkItem is soft-deleted.
        builder.HasOne(h => h.WorkItem)
            .WithMany(w => w.Histories)
            .HasForeignKey(h => h.WorkItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => new { h.WorkItemId, h.CreatedAt });
        builder.HasIndex(h => new { h.ActorId, h.CreatedAt });
    }
}
