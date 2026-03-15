using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class AiInteractionConfiguration : IEntityTypeConfiguration<AiInteraction>
{
    public void Configure(EntityTypeBuilder<AiInteraction> builder)
    {
        builder.ToTable("ai_interactions");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.FeatureType).HasColumnName("feature_type").HasMaxLength(50).IsRequired();
        builder.Property(a => a.WorkItemId).HasColumnName("work_item_id");
        builder.Property(a => a.SprintId).HasColumnName("sprint_id");
        builder.Property(a => a.ModelVersion).HasColumnName("model_version").HasMaxLength(100);
        builder.Property(a => a.InputContext).HasColumnName("input_context").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.AiOutput).HasColumnName("ai_output").HasColumnType("jsonb").IsRequired();
        builder.Property(a => a.UserAction).HasColumnName("user_action").HasMaxLength(20).IsRequired();
        builder.Property(a => a.UserModified).HasColumnName("user_modified").HasColumnType("jsonb");
        builder.Property(a => a.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(a => a.LatencyMs).HasColumnName("latency_ms");
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz");

        builder.HasOne(a => a.WorkItem)
            .WithMany()
            .HasForeignKey(a => a.WorkItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Sprint)
            .WithMany()
            .HasForeignKey(a => a.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
