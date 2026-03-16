using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("email_outbox");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.RecipientEmail).HasColumnName("recipient_email").HasMaxLength(255).IsRequired();
        builder.Property(e => e.RecipientId).HasColumnName("recipient_id");
        builder.Property(e => e.TemplateType).HasColumnName("template_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(500).IsRequired();
        builder.Property(e => e.BodyJson).HasColumnName("body_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(e => e.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(e => e.MaxAttempts).HasColumnName("max_attempts").IsRequired();
        builder.Property(e => e.NextRetryAt).HasColumnName("next_retry_at").HasColumnType("timestamptz");
        builder.Property(e => e.LastError).HasColumnName("last_error");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(e => e.SentAt).HasColumnName("sent_at").HasColumnType("timestamptz");

        builder.HasOne(e => e.Recipient)
            .WithMany()
            .HasForeignKey(e => e.RecipientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
            .HasFilter("status IN ('Pending', 'Failed')");
    }
}
