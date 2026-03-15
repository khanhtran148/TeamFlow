using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class RetroActionItemConfiguration : IEntityTypeConfiguration<RetroActionItem>
{
    public void Configure(EntityTypeBuilder<RetroActionItem> builder)
    {
        builder.ToTable("retro_action_items");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(a => a.CardId).HasColumnName("card_id");
        builder.Property(a => a.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description");
        builder.Property(a => a.AssigneeId).HasColumnName("assignee_id");
        builder.Property(a => a.DueDate).HasColumnName("due_date");
        builder.Property(a => a.LinkedTaskId).HasColumnName("linked_task_id");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne(a => a.Session)
            .WithMany(s => s.ActionItems)
            .HasForeignKey(a => a.SessionId);

        builder.HasOne(a => a.Card)
            .WithMany(c => c.ActionItems)
            .HasForeignKey(a => a.CardId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Assignee)
            .WithMany()
            .HasForeignKey(a => a.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.LinkedTask)
            .WithMany()
            .HasForeignKey(a => a.LinkedTaskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
