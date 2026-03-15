using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class ProjectMembershipConfiguration : IEntityTypeConfiguration<ProjectMembership>
{
    public void Configure(EntityTypeBuilder<ProjectMembership> builder)
    {
        builder.ToTable("project_memberships");

        builder.HasKey(pm => pm.Id);
        builder.Property(pm => pm.Id).HasColumnName("id");
        builder.Property(pm => pm.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(pm => pm.MemberId).HasColumnName("member_id").IsRequired();
        builder.Property(pm => pm.MemberType).HasColumnName("member_type").HasMaxLength(10).IsRequired();
        builder.Property(pm => pm.Role).HasColumnName("role").HasConversion<string>().IsRequired();
        builder.Property(pm => pm.CustomPermissions).HasColumnName("custom_permissions")
            .HasColumnType("jsonb");
        builder.Property(pm => pm.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex(pm => new { pm.ProjectId, pm.MemberId, pm.MemberType }).IsUnique();

        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.Memberships)
            .HasForeignKey(pm => pm.ProjectId);
    }
}
