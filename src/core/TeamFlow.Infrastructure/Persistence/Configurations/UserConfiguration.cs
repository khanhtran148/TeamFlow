using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(u => u.SystemRole).HasColumnName("system_role").HasDefaultValue(TeamFlow.Domain.Enums.SystemRole.User);
        builder.Property(u => u.MustChangePassword).HasColumnName("must_change_password").HasDefaultValue(false);
        builder.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
