using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        builder.ToTable("in_app_notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.RecipientId).HasColumnName("recipient_id").IsRequired();
        builder.Property(n => n.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(n => n.Body).HasColumnName("body").HasMaxLength(2000);
        builder.Property(n => n.ReferenceId).HasColumnName("reference_id");
        builder.Property(n => n.ReferenceType).HasColumnName("reference_type").HasMaxLength(50);
        builder.Property(n => n.IsRead).HasColumnName("is_read").IsRequired();
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt })
            .IsDescending(false, false, true);
    }
}
