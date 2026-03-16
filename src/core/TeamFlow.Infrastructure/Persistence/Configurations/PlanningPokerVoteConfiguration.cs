using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class PlanningPokerVoteConfiguration : IEntityTypeConfiguration<PlanningPokerVote>
{
    public void Configure(EntityTypeBuilder<PlanningPokerVote> builder)
    {
        builder.ToTable("planning_poker_votes");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(v => v.VoterId).HasColumnName("voter_id").IsRequired();
        builder.Property(v => v.Value).HasColumnName("value").HasColumnType("decimal(5,1)").IsRequired();
        builder.Property(v => v.VotedAt).HasColumnName("voted_at").HasColumnType("timestamptz");

        builder.HasOne(v => v.Session)
            .WithMany(s => s.Votes)
            .HasForeignKey(v => v.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Voter)
            .WithMany()
            .HasForeignKey(v => v.VoterId)
            .OnDelete(DeleteBehavior.Restrict);

        // One vote per voter per session
        builder.HasIndex(v => new { v.SessionId, v.VoterId }).IsUnique();
    }
}
