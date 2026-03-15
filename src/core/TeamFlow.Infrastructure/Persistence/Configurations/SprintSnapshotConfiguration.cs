using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class SprintSnapshotConfiguration : IEntityTypeConfiguration<SprintSnapshot>
{
    public void Configure(EntityTypeBuilder<SprintSnapshot> builder)
    {
        builder.ToTable("sprint_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.SprintId).HasColumnName("sprint_id").IsRequired();
        builder.Property(s => s.SnapshotType).HasColumnName("snapshot_type").HasMaxLength(20).IsRequired();
        builder.Property(s => s.IsFinal).HasColumnName("is_final").IsRequired();
        builder.Property(s => s.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.CapturedAt).HasColumnName("captured_at").HasColumnType("timestamptz");

        builder.HasIndex(s => new { s.SprintId, s.SnapshotType });

        builder.HasOne(s => s.Sprint)
            .WithMany(s => s.Snapshots)
            .HasForeignKey(s => s.SprintId);
    }
}
