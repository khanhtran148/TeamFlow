using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class RetroCardConfiguration : IEntityTypeConfiguration<RetroCard>
{
    public void Configure(EntityTypeBuilder<RetroCard> builder)
    {
        builder.ToTable("retro_cards");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(c => c.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(c => c.Category).HasColumnName("category")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Content).HasColumnName("content").IsRequired();
        builder.Property(c => c.IsDiscussed).HasColumnName("is_discussed").IsRequired();
        builder.Property(c => c.Sentiment).HasColumnName("sentiment");
        builder.Property(c => c.ThemeTags).HasColumnName("theme_tags").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasOne(c => c.Session)
            .WithMany(s => s.Cards)
            .HasForeignKey(c => c.SessionId);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
