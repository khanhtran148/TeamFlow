using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class TeamHealthSummaryConfiguration : IEntityTypeConfiguration<TeamHealthSummary>
{
    public void Configure(EntityTypeBuilder<TeamHealthSummary> builder)
    {
        builder.ToTable("team_health_summaries");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(s => s.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(s => s.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(s => s.SummaryData).HasColumnName("summary_data").HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.GeneratedAt).HasColumnName("generated_at").HasColumnType("timestamptz");

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ProjectId, s.PeriodStart }).IsUnique();
        builder.HasIndex(s => new { s.ProjectId, s.PeriodStart })
            .IsDescending(false, true)
            .HasDatabaseName("idx_ths_project");
    }
}
