using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.OrgId).HasColumnName("org_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasColumnName("description");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne(t => t.Organization)
            .WithMany(o => o.Teams)
            .HasForeignKey(t => t.OrgId);
    }
}
