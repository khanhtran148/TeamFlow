using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.NotificationType).HasColumnName("notification_type").HasMaxLength(50).IsRequired();
        builder.Property(p => p.EmailEnabled).HasColumnName("email_enabled").IsRequired();
        builder.Property(p => p.InAppEnabled).HasColumnName("in_app_enabled").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.NotificationType }).IsUnique();
        builder.HasIndex(p => p.UserId);
    }
}
