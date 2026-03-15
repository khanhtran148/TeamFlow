using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class DomainEventConfiguration : IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(EntityTypeBuilder<DomainEvent> builder)
    {
        builder.ToTable("domain_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(e => e.AggregateType).HasColumnName("aggregate_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(e => e.ActorId).HasColumnName("actor_id");
        builder.Property(e => e.ActorType).HasColumnName("actor_type").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.SchemaVersion).HasColumnName("schema_version").IsRequired();
        builder.Property(e => e.SessionId).HasColumnName("session_id");

        builder.HasIndex(e => new { e.AggregateType, e.AggregateId, e.OccurredAt });
        builder.HasIndex(e => new { e.ActorId, e.EventType, e.OccurredAt });
        builder.HasIndex(e => e.OccurredAt);
    }
}
