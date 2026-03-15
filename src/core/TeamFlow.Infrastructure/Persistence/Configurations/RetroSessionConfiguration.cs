using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class RetroSessionConfiguration : IEntityTypeConfiguration<RetroSession>
{
    public void Configure(EntityTypeBuilder<RetroSession> builder)
    {
        builder.ToTable("retro_sessions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.SprintId).HasColumnName("sprint_id");
        builder.Property(r => r.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(r => r.FacilitatorId).HasColumnName("facilitator_id").IsRequired();
        builder.Property(r => r.AnonymityMode).HasColumnName("anonymity_mode").HasMaxLength(10).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.AiSummary).HasColumnName("ai_summary").HasColumnType("jsonb");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne(r => r.Sprint)
            .WithMany()
            .HasForeignKey(r => r.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Project)
            .WithMany(p => p.RetroSessions)
            .HasForeignKey(r => r.ProjectId);

        builder.HasOne(r => r.Facilitator)
            .WithMany()
            .HasForeignKey(r => r.FacilitatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
