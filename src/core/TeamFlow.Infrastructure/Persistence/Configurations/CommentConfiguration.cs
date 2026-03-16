using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.WorkItemId).HasColumnName("work_item_id").IsRequired();
        builder.Property(c => c.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(c => c.ParentCommentId).HasColumnName("parent_comment_id");
        builder.Property(c => c.Content).HasColumnName("content").HasMaxLength(10000).IsRequired();
        builder.Property(c => c.EditedAt).HasColumnName("edited_at").HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");

        builder.HasOne(c => c.WorkItem)
            .WithMany()
            .HasForeignKey(c => c.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.WorkItemId, c.CreatedAt })
            .IsDescending(false, true);

        builder.HasIndex(c => c.ParentCommentId);

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
