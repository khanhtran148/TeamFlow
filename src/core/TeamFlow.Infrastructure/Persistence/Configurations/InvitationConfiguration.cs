using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.OrganizationId).HasColumnName("organization_id").IsRequired();
        builder.Property(i => i.InvitedByUserId).HasColumnName("invited_by_user_id").IsRequired();
        builder.Property(i => i.Email).HasColumnName("email").HasMaxLength(256);
        builder.Property(i => i.Role).HasColumnName("role").HasConversion<string>().IsRequired();
        builder.Property(i => i.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(i => i.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(i => i.AcceptedAt).HasColumnName("accepted_at").HasColumnType("timestamptz");
        builder.Property(i => i.AcceptedByUserId).HasColumnName("accepted_by_user_id");

        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => i.Email);
        builder.HasIndex(i => new { i.OrganizationId, i.Status });

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AcceptedBy)
            .WithMany()
            .HasForeignKey(i => i.AcceptedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
