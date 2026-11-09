using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Messaging.Infrastructure.Persistence.Configurations;

public class MessageDraftConfiguration : IEntityTypeConfiguration<MessageDraft>
{
    public void Configure(EntityTypeBuilder<MessageDraft> builder)
    {
        builder.ToTable("message_drafts", t =>
        {
            t.HasCheckConstraint("ck_drafts_source", "source IN ('MANUAL', 'AI_ASSISTANT')");
        });

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(d => d.AuthorId)
            .HasColumnName("author_id")
            .IsRequired();

        builder.Property(d => d.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .HasDefaultValue(MessageDraftSource.Manual)
            .IsRequired();

        builder.Property(d => d.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(d => new { d.ConversationId, d.AuthorId })
            .IsUnique()
            .HasDatabaseName("uq_drafts_author");

        builder.HasOne(d => d.Conversation)
            .WithMany(c => c.Drafts)
            .HasForeignKey(d => d.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
