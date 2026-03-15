using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class WorkItemLinkConfiguration : IEntityTypeConfiguration<WorkItemLink>
{
    public void Configure(EntityTypeBuilder<WorkItemLink> builder)
    {
        builder.ToTable("work_item_links");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.SourceId).HasColumnName("source_id").IsRequired();
        builder.Property(l => l.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(l => l.LinkType).HasColumnName("link_type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(l => l.Scope).HasColumnName("scope")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.CreatedById).HasColumnName("created_by").IsRequired();
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(l => new { l.SourceId, l.TargetId, l.LinkType }).IsUnique();
        builder.HasIndex(l => l.SourceId);
        builder.HasIndex(l => l.TargetId);

        builder.HasOne(l => l.Source)
            .WithMany(w => w.SourceLinks)
            .HasForeignKey(l => l.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Target)
            .WithMany(w => w.TargetLinks)
            .HasForeignKey(l => l.TargetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.CreatedBy)
            .WithMany()
            .HasForeignKey(l => l.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => l.Source!.DeletedAt == null && l.Target!.DeletedAt == null);
    }
}
