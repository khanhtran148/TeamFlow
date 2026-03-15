using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class TeamVelocityHistoryConfiguration : IEntityTypeConfiguration<TeamVelocityHistory>
{
    public void Configure(EntityTypeBuilder<TeamVelocityHistory> builder)
    {
        builder.ToTable("team_velocity_history");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(v => v.SprintId).HasColumnName("sprint_id").IsRequired();
        builder.Property(v => v.PlannedPoints).HasColumnName("planned_points").IsRequired();
        builder.Property(v => v.CompletedPoints).HasColumnName("completed_points").IsRequired();
        builder.Property(v => v.Velocity).HasColumnName("velocity").IsRequired();
        builder.Property(v => v.Velocity3SprintAvg).HasColumnName("velocity_3sprint_avg");
        builder.Property(v => v.Velocity6SprintAvg).HasColumnName("velocity_6sprint_avg");
        builder.Property(v => v.VelocityTrend).HasColumnName("velocity_trend").HasMaxLength(20);
        builder.Property(v => v.AiAdjustedVelocity).HasColumnName("ai_adjusted_velocity");
        builder.Property(v => v.ConfidenceInterval).HasColumnName("confidence_interval").HasColumnType("jsonb");
        builder.Property(v => v.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz");

        builder.HasOne(v => v.Project)
            .WithMany()
            .HasForeignKey(v => v.ProjectId);

        builder.HasOne(v => v.Sprint)
            .WithMany()
            .HasForeignKey(v => v.SprintId);
    }
}
