using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Messaging.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialMessagingSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "messaging");

        migrationBuilder.CreateTable(
            name: "conversations",
            schema: "messaging",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                initiator_id = table.Column<long>(type: "bigint", nullable: false),
                recipient_id = table.Column<long>(type: "bigint", nullable: false),
                listing_id = table.Column<long>(type: "bigint", nullable: true),
                roommate_post_id = table.Column<long>(type: "bigint", nullable: true),
                ai_session_id = table.Column<long>(type: "bigint", nullable: true),
                conversation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "LISTING"),
                user_min_id = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "LEAST(initiator_id, recipient_id)", stored: true),
                user_max_id = table.Column<long>(type: "bigint", nullable: false, computedColumnSql: "GREATEST(initiator_id, recipient_id)", stored: true),
                last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_conversations", x => x.id);
                table.CheckConstraint("ck_conversations_parties", "initiator_id <> recipient_id");
                table.CheckConstraint("ck_conversations_type", "conversation_type IN ('LISTING', 'ROOMMATE', 'AI_ESCALATED')");
            });

        migrationBuilder.CreateTable(
            name: "leads",
            schema: "messaging",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                tenant_id = table.Column<long>(type: "bigint", nullable: false),
                listing_id = table.Column<long>(type: "bigint", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NEW"),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "DIRECT"),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_leads", x => x.id);
                table.CheckConstraint("ck_leads_source", "source IN ('DIRECT', 'SEARCH_FILTER', 'COMPARISON', 'AI_CHAT')");
                table.CheckConstraint("ck_leads_status", "status IN ('NEW', 'CONTACTED', 'VIEWING_SCHEDULED', 'CONTRACT_SIGNED', 'LOST')");
            });

        migrationBuilder.CreateTable(
            name: "message_drafts",
            schema: "messaging",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                conversation_id = table.Column<long>(type: "bigint", nullable: false),
                author_id = table.Column<long>(type: "bigint", nullable: false),
                source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "MANUAL"),
                content = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_message_drafts", x => x.id);
                table.CheckConstraint("ck_drafts_source", "source IN ('MANUAL', 'AI_ASSISTANT')");
                table.ForeignKey(
                    name: "FK_message_drafts_conversations_conversation_id",
                    column: x => x.conversation_id,
                    principalSchema: "messaging",
                    principalTable: "conversations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "messages",
            schema: "messaging",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                conversation_id = table.Column<long>(type: "bigint", nullable: false),
                sender_id = table.Column<long>(type: "bigint", nullable: false),
                message_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "TEXT"),
                content = table.Column<string>(type: "text", nullable: false),
                is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_messages", x => x.id);
                table.CheckConstraint("ck_messages_type", "message_type IN ('TEXT', 'IMAGE', 'SYSTEM', 'AI_SUGGESTION')");
                table.ForeignKey(
                    name: "FK_messages_conversations_conversation_id",
                    column: x => x.conversation_id,
                    principalSchema: "messaging",
                    principalTable: "conversations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_conversations_initiator_id",
            schema: "messaging",
            table: "conversations",
            column: "initiator_id");

        migrationBuilder.CreateIndex(
            name: "ix_conversations_recipient_id",
            schema: "messaging",
            table: "conversations",
            column: "recipient_id");

        migrationBuilder.CreateIndex(
            name: "uq_conversations_listingchat",
            schema: "messaging",
            table: "conversations",
            columns: new[] { "user_min_id", "user_max_id", "listing_id" },
            unique: true,
            filter: "listing_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "uq_conversations_roommatechat",
            schema: "messaging",
            table: "conversations",
            columns: new[] { "user_min_id", "user_max_id", "roommate_post_id" },
            unique: true,
            filter: "roommate_post_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "uq_leads_tenant_listing",
            schema: "messaging",
            table: "leads",
            columns: new[] { "tenant_id", "listing_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "uq_drafts_author",
            schema: "messaging",
            table: "message_drafts",
            columns: new[] { "conversation_id", "author_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_messages_conversation_created_at",
            schema: "messaging",
            table: "messages",
            columns: new[] { "conversation_id", "created_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "leads",
            schema: "messaging");

        migrationBuilder.DropTable(
            name: "message_drafts",
            schema: "messaging");

        migrationBuilder.DropTable(
            name: "messages",
            schema: "messaging");

        migrationBuilder.DropTable(
            name: "conversations",
            schema: "messaging");
    }
}
