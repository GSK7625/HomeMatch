using HomeMatch.Modules.Messaging.Domain.Entities;
using HomeMatch.Modules.Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMatch.Modules.Messaging.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", t =>
        {
            t.HasCheckConstraint("ck_conversations_type", "conversation_type IN ('LISTING', 'ROOMMATE', 'AI_ESCALATED')");
            t.HasCheckConstraint("ck_conversations_parties", "initiator_id <> recipient_id");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.InitiatorId)
            .HasColumnName("initiator_id")
            .IsRequired();

        builder.Property(c => c.RecipientId)
            .HasColumnName("recipient_id")
            .IsRequired();

        builder.Property(c => c.ListingId)
            .HasColumnName("listing_id");

        builder.Property(c => c.RoommatePostId)
            .HasColumnName("roommate_post_id");

        builder.Property(c => c.AiSessionId)
            .HasColumnName("ai_session_id");

        builder.Property(c => c.ConversationType)
            .HasColumnName("conversation_type")
            .HasMaxLength(20)
            .HasDefaultValue(ConversationType.Listing)
            .IsRequired();

        builder.Property(c => c.UserMinId)
            .HasColumnName("user_min_id")
            .HasComputedColumnSql("LEAST(initiator_id, recipient_id)", stored: true);

        builder.Property(c => c.UserMaxId)
            .HasColumnName("user_max_id")
            .HasComputedColumnSql("GREATEST(initiator_id, recipient_id)", stored: true);

        builder.Property(c => c.LastMessageAt)
            .HasColumnName("last_message_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(c => new { c.UserMinId, c.UserMaxId, c.ListingId })
            .IsUnique()
            .HasDatabaseName("uq_conversations_listingchat")
            .HasFilter("listing_id IS NOT NULL");

        builder.HasIndex(c => new { c.UserMinId, c.UserMaxId, c.RoommatePostId })
            .IsUnique()
            .HasDatabaseName("uq_conversations_roommatechat")
            .HasFilter("roommate_post_id IS NOT NULL");

        builder.HasIndex(c => c.InitiatorId)
            .HasDatabaseName("ix_conversations_initiator_id");

        builder.HasIndex(c => c.RecipientId)
            .HasDatabaseName("ix_conversations_recipient_id");
    }
}
