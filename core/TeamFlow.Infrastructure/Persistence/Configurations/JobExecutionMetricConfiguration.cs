using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class JobExecutionMetricConfiguration : IEntityTypeConfiguration<JobExecutionMetric>
{
    public void Configure(EntityTypeBuilder<JobExecutionMetric> builder)
    {
        builder.ToTable("job_execution_metrics");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.JobType).HasColumnName("job_type").HasMaxLength(100).IsRequired();
        builder.Property(j => j.JobRunId).HasColumnName("job_run_id").IsRequired();
        builder.Property(j => j.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(j => j.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(j => j.DurationMs).HasColumnName("duration_ms");
        builder.Property(j => j.RecordsProcessed).HasColumnName("records_processed");
        builder.Property(j => j.RecordsFailed).HasColumnName("records_failed");
        builder.Property(j => j.ErrorMessage).HasColumnName("error_message");
        builder.Property(j => j.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
    }
}
