using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "email_verification_quotas",
                schema: "identity",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    window_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_quotas", x => x.key);
                    table.CheckConstraint("CK_EmailVerificationQuota_Count", "\"count\" >= 0");
                    table.CheckConstraint("CK_EmailVerificationQuota_Window", "\"window_end\" > \"window_start\"");
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    security_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    security_stamp = table.Column<string>(type: "text", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("CK_Users_SecurityVersion", "\"security_version\" > 0");
                    table.CheckConstraint("CK_Users_Status", "\"status\" IN ('ACTIVE', 'INACTIVE', 'LOCKED')");
                    table.CheckConstraint("CK_Users_Timestamps", "\"updated_at\" >= \"created_at\"");
                });

            migrationBuilder.CreateTable(
                name: "email_verification_challenges",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email_snapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_email_snapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    code_mac = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    pending_lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invalidated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_challenges", x => x.id);
                    table.CheckConstraint("CK_EmailVerification_Attempts", "\"failed_attempts\" BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_EmailVerification_Consumed", "(\"state\" = 4) = (\"consumed_at\" IS NOT NULL) AND (\"consumed_at\" IS NULL OR (\"consumed_at\" >= \"created_at\" AND \"consumed_at\" < \"expires_at\"))");
                    table.CheckConstraint("CK_EmailVerification_Invalidated", "(\"state\" IN (3,5,6)) = (\"invalidated_at\" IS NOT NULL) AND (\"invalidated_at\" IS NULL OR \"invalidated_at\" >= \"created_at\")");
                    table.CheckConstraint("CK_EmailVerification_KeyId", "\"key_id\" ~ '^[A-Za-z0-9_-]{1,32}$'");
                    table.CheckConstraint("CK_EmailVerification_LastAttempt", "\"last_attempt_at\" IS NULL OR \"last_attempt_at\" >= \"created_at\"");
                    table.CheckConstraint("CK_EmailVerification_Locked", "(\"state\" = 6) = (\"failed_attempts\" = 5)");
                    table.CheckConstraint("CK_EmailVerification_Mac", "\"code_mac\" ~ '^[0-9A-F]{64}$'");
                    table.CheckConstraint("CK_EmailVerification_Purpose", "\"purpose\" = 'EmailVerification'");
                    table.CheckConstraint("CK_EmailVerification_State", "\"state\" BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_EmailVerification_Submitted", "(\"state\" NOT IN (2,4,6) OR \"submitted_at\" IS NOT NULL) AND (\"submitted_at\" IS NULL OR (\"submitted_at\" >= \"created_at\" AND \"submitted_at\" < \"expires_at\"))");
                    table.CheckConstraint("CK_EmailVerification_Time", "\"expires_at\" = \"created_at\" + interval '5 minutes' AND \"pending_lease_until\" = \"created_at\" + interval '2 minutes'");
                    table.ForeignKey(
                        name: "FK_email_verification_challenges_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "public_otp_challenges",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    security_version = table.Column<long>(type: "bigint", nullable: false),
                    code_mac = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted = table.Column<bool>(type: "boolean", nullable: false),
                    closed = table.Column<bool>(type: "boolean", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    reset_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reset_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reset_used = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_otp_challenges", x => x.id);
                    table.ForeignKey(
                        name: "FK_public_otp_challenges_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    security_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.CheckConstraint("CK_Sessions_Expiry", "\"expires_at\" > \"created_at\"");
                    table.CheckConstraint("CK_Sessions_Revocation", "\"revoked_at\" IS NULL OR \"revoked_at\" >= \"created_at\"");
                    table.CheckConstraint("CK_Sessions_SecurityVersion", "\"security_version\" > 0");
                    table.ForeignKey(
                        name: "FK_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    notify_email = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_sms = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notify_push = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.user_id);
                    table.CheckConstraint("CK_UserProfiles_Name", "char_length(btrim(\"full_name\")) BETWEEN 2 AND 100");
                    table.ForeignKey(
                        name: "FK_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", nullable: false),
                    token_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    device_info = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_credential_metadata",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_credential_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_credential_metadata", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_credential_metadata_refresh_credential_metadata_rep~",
                        column: x => x.replaced_by_credential_id,
                        principalSchema: "identity",
                        principalTable: "refresh_credential_metadata",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_refresh_credential_metadata_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "identity",
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_refresh_credential_metadata_user_tokens_id",
                        column: x => x.id,
                        principalSchema: "identity",
                        principalTable: "user_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_challenges_user_id_created_at",
                schema: "identity",
                table: "email_verification_challenges",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "UX_EmailVerificationChallenges_Active",
                schema: "identity",
                table: "email_verification_challenges",
                columns: new[] { "user_id", "purpose" },
                unique: true,
                filter: "\"state\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_public_otp_challenges_reset_token_hash",
                schema: "identity",
                table: "public_otp_challenges",
                column: "reset_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_public_otp_challenges_user_id_purpose_created_at",
                schema: "identity",
                table: "public_otp_challenges",
                columns: new[] { "user_id", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_credential_metadata_session_id",
                schema: "identity",
                table: "refresh_credential_metadata",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "refresh_credential_metadata",
                column: "session_id",
                unique: true,
                filter: "\"used_at\" IS NULL AND \"revoked_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "refresh_credential_metadata",
                column: "replaced_by_credential_id",
                unique: true,
                filter: "\"replaced_by_credential_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id",
                schema: "identity",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_tokens_token_hash",
                schema: "identity",
                table: "user_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_tokens_user_id",
                schema: "identity",
                table: "user_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_phone",
                schema: "identity",
                table: "users",
                column: "phone_number",
                unique: true,
                filter: "\"phone_number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedEmail",
                schema: "identity",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.Sql("ALTER TABLE identity.users ADD CONSTRAINT ck_users_role CHECK (role IN ('TENANT','LANDLORD','ADMIN'));");
            migrationBuilder.Sql("ALTER TABLE identity.user_tokens ADD CONSTRAINT ck_usertokens_type CHECK (token_type IN ('REFRESH','RESET_PASSWORD','REMEMBER_ME'));");
            migrationBuilder.Sql("ALTER TABLE identity.user_profiles ADD CONSTRAINT ck_userprofiles_gender CHECK (gender IN ('MALE','FEMALE','OTHER'));");
            migrationBuilder.Sql("""
                CREATE FUNCTION identity.check_refresh_family() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM identity.user_tokens t
                        JOIN identity.sessions s ON s.user_id = t.user_id
                        WHERE t.id = NEW.id AND s.id = NEW.session_id AND t.token_type = 'REFRESH'
                    ) OR (
                        NEW.replaced_by_credential_id IS NOT NULL AND NOT EXISTS (
                            SELECT 1 FROM identity.refresh_credential_metadata r
                            WHERE r.id = NEW.replaced_by_credential_id AND r.session_id = NEW.session_id
                        )
                    ) THEN
                        RAISE EXCEPTION 'Refresh credential family mismatch' USING ERRCODE = '23503';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER check_refresh_family BEFORE INSERT OR UPDATE
                ON identity.refresh_credential_metadata FOR EACH ROW
                EXECUTE FUNCTION identity.check_refresh_family();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION identity.check_refresh_family() CASCADE;");
            migrationBuilder.DropTable(
                name: "email_verification_challenges",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "email_verification_quotas",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "public_otp_challenges",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "refresh_credential_metadata",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");
        }
    }
}
