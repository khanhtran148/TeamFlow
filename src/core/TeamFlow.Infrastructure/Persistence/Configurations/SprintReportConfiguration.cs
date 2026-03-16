using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class SprintReportConfiguration : IEntityTypeConfiguration<SprintReport>
{
    public void Configure(EntityTypeBuilder<SprintReport> builder)
    {
        builder.ToTable("sprint_reports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.SprintId).HasColumnName("sprint_id").IsRequired();
        builder.Property(r => r.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(r => r.ReportData).HasColumnName("report_data").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.GeneratedAt).HasColumnName("generated_at").HasColumnType("timestamptz");
        builder.Property(r => r.GeneratedBy).HasColumnName("generated_by").HasMaxLength(20).IsRequired();

        builder.HasOne(r => r.Sprint)
            .WithMany()
            .HasForeignKey(r => r.SprintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.SprintId).IsUnique();
    }
}
