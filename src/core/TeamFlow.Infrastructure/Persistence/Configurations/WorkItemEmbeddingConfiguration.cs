using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public class WorkItemEmbeddingConfiguration : IEntityTypeConfiguration<WorkItemEmbedding>
{
    public void Configure(EntityTypeBuilder<WorkItemEmbedding> builder)
    {
        builder.ToTable("work_item_embeddings");

        builder.HasKey(e => e.WorkItemId);
        builder.Property(e => e.WorkItemId).HasColumnName("work_item_id");
        // Embedding stored as float array — pgvector support requires Npgsql.EntityFrameworkCore.PostgreSQL with vector extension
        // For Phase 0, store as text/bytea; pgvector integration added in Phase 5
        builder.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("float4[]");
        builder.Property(e => e.Model).HasColumnName("model").HasMaxLength(100);
        builder.Property(e => e.GeneratedAt).HasColumnName("generated_at").HasColumnType("timestamptz");
        builder.Property(e => e.IsStale).HasColumnName("is_stale").IsRequired();

        builder.HasOne(e => e.WorkItem)
            .WithOne(w => w.Embedding)
            .HasForeignKey<WorkItemEmbedding>(e => e.WorkItemId);
    }
}
