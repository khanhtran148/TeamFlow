using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members");

        builder.HasKey(tm => tm.Id);
        builder.Property(tm => tm.Id).HasColumnName("id");
        builder.Property(tm => tm.TeamId).HasColumnName("team_id").IsRequired();
        builder.Property(tm => tm.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(tm => tm.Role).HasColumnName("role")
            .HasConversion<string>().IsRequired();
        builder.Property(tm => tm.JoinedAt).HasColumnName("joined_at").HasColumnType("timestamptz");

        builder.HasIndex(tm => new { tm.TeamId, tm.UserId }).IsUnique();

        builder.HasOne(tm => tm.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(tm => tm.TeamId);

        builder.HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId);
    }
}
