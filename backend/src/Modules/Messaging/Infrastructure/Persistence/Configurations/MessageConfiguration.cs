using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Messaging.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", t =>
        {
            t.HasCheckConstraint("ck_messages_type", "message_type IN ('TEXT', 'IMAGE', 'SYSTEM', 'AI_SUGGESTION')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.SenderId)
            .HasColumnName("sender_id")
            .IsRequired();

        builder.Property(m => m.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(20)
            .HasDefaultValue(MessageType.Text)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .HasDatabaseName("ix_messages_conversation_created_at");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
