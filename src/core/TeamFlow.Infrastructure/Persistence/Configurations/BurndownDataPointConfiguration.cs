using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class BurndownDataPointConfiguration : IEntityTypeConfiguration<BurndownDataPoint>
{
    public void Configure(EntityTypeBuilder<BurndownDataPoint> builder)
    {
        builder.ToTable("burndown_data_points");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.SprintId).HasColumnName("sprint_id").IsRequired();
        builder.Property(b => b.RecordedDate).HasColumnName("recorded_date").IsRequired();
        builder.Property(b => b.RemainingPoints).HasColumnName("remaining_points").IsRequired();
        builder.Property(b => b.CompletedPoints).HasColumnName("completed_points").IsRequired();
        builder.Property(b => b.AddedPoints).HasColumnName("added_points").IsRequired();
        builder.Property(b => b.IsWeekend).HasColumnName("is_weekend").IsRequired();
        builder.Property(b => b.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz");

        builder.HasIndex(b => new { b.SprintId, b.RecordedDate }).IsUnique();

        builder.HasOne(b => b.Sprint)
            .WithMany(s => s.BurndownDataPoints)
            .HasForeignKey(b => b.SprintId);
    }
}
