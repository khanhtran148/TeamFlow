using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("releases");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description");
        builder.Property(r => r.ReleaseDate).HasColumnName("release_date");
        builder.Property(r => r.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.ReleasedAt).HasColumnName("released_at").HasColumnType("timestamptz");
        builder.Property(r => r.ReleasedById).HasColumnName("released_by_id");
        builder.Property(r => r.ReleaseNotes).HasColumnName("release_notes");
        builder.Property(r => r.NotesLocked).HasColumnName("notes_locked").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne(r => r.Project)
            .WithMany(p => p.Releases)
            .HasForeignKey(r => r.ProjectId);

        builder.HasOne(r => r.ReleasedBy)
            .WithMany()
            .HasForeignKey(r => r.ReleasedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
