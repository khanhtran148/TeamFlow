using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class SavedFilterConfiguration : IEntityTypeConfiguration<SavedFilter>
{
    public void Configure(EntityTypeBuilder<SavedFilter> builder)
    {
        builder.ToTable("saved_filters");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(f => f.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(f => f.FilterJson).HasColumnName("filter_json").HasColumnType("jsonb").IsRequired();
        builder.Property(f => f.IsDefault).HasColumnName("is_default").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Project)
            .WithMany()
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.UserId, f.ProjectId, f.Name }).IsUnique();
        builder.HasIndex(f => new { f.UserId, f.ProjectId });
    }
}
