using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class RetroVoteConfiguration : IEntityTypeConfiguration<RetroVote>
{
    public void Configure(EntityTypeBuilder<RetroVote> builder)
    {
        builder.ToTable("retro_votes");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.CardId).HasColumnName("card_id").IsRequired();
        builder.Property(v => v.VoterId).HasColumnName("voter_id").IsRequired();
        builder.Property(v => v.VoteCount).HasColumnName("vote_count").IsRequired();

        builder.HasIndex(v => new { v.CardId, v.VoterId }).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CK_retro_votes_vote_count", "vote_count BETWEEN 1 AND 2"));

        builder.HasOne(v => v.Card)
            .WithMany(c => c.Votes)
            .HasForeignKey(v => v.CardId);

        builder.HasOne(v => v.Voter)
            .WithMany()
            .HasForeignKey(v => v.VoterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
