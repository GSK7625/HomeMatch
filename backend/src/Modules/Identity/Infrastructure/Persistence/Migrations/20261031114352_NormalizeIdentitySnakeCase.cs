using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeIdentitySnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailVerificationChallenges_Users_UserId",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicOtpChallenges_Users_UserId",
                schema: "identity",
                table: "PublicOtpChallenges");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshCredentials_RefreshCredentials_ReplacedByCredentialI~",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshCredentials_Sessions_SessionId",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                schema: "identity",
                table: "RoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Users_UserId",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserClaims_Users_UserId",
                schema: "identity",
                table: "UserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLogins_Users_UserId",
                schema: "identity",
                table: "UserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_Users_UserId",
                schema: "identity",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "identity",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "identity",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTokens_Users_UserId",
                schema: "identity",
                table: "UserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_SecurityVersion",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Status",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Timestamps",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sessions",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_Expiry",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_Revocation",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_SecurityVersion",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                schema: "identity",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTokens",
                schema: "identity",
                table: "UserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                schema: "identity",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserProfiles",
                schema: "identity",
                table: "UserProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProfiles_Name",
                schema: "identity",
                table: "UserProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProfiles_Timestamps",
                schema: "identity",
                table: "UserProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserLogins",
                schema: "identity",
                table: "UserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserClaims",
                schema: "identity",
                table: "UserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleClaims",
                schema: "identity",
                table: "RoleClaims");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RefreshCredentials_Id_SessionId",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshCredentials",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Expiry",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Hash",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Revoked",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Used",
                schema: "identity",
                table: "RefreshCredentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PublicOtpChallenges",
                schema: "identity",
                table: "PublicOtpChallenges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailVerificationQuotas",
                schema: "identity",
                table: "EmailVerificationQuotas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerificationQuota_Count",
                schema: "identity",
                table: "EmailVerificationQuotas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerificationQuota_Window",
                schema: "identity",
                table: "EmailVerificationQuotas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailVerificationChallenges",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropIndex(
                name: "UX_EmailVerificationChallenges_Active",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Attempts",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Consumed",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Invalidated",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_KeyId",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_LastAttempt",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Locked",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Mac",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Purpose",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_State",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Submitted",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Time",
                schema: "identity",
                table: "EmailVerificationChallenges");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "identity",
                newName: "users",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "Sessions",
                schema: "identity",
                newName: "sessions",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "identity",
                newName: "roles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserTokens",
                schema: "identity",
                newName: "user_tokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "identity",
                newName: "user_roles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserProfiles",
                schema: "identity",
                newName: "user_profiles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserLogins",
                schema: "identity",
                newName: "user_logins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "UserClaims",
                schema: "identity",
                newName: "user_claims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "RoleClaims",
                schema: "identity",
                newName: "role_claims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "RefreshCredentials",
                schema: "identity",
                newName: "refresh_credentials",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "PublicOtpChallenges",
                schema: "identity",
                newName: "public_otp_challenges",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "EmailVerificationQuotas",
                schema: "identity",
                newName: "email_verification_quotas",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "EmailVerificationChallenges",
                schema: "identity",
                newName: "email_verification_challenges",
                newSchema: "identity");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "identity",
                table: "users",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Email",
                schema: "identity",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                schema: "identity",
                table: "users",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                schema: "identity",
                table: "users",
                newName: "two_factor_enabled");

            migrationBuilder.RenameColumn(
                name: "SecurityVersion",
                schema: "identity",
                table: "users",
                newName: "security_version");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                schema: "identity",
                table: "users",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                schema: "identity",
                table: "users",
                newName: "phone_number_confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                schema: "identity",
                table: "users",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                schema: "identity",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                schema: "identity",
                table: "users",
                newName: "normalized_user_name");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                schema: "identity",
                table: "users",
                newName: "normalized_email");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                schema: "identity",
                table: "users",
                newName: "lockout_end");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                schema: "identity",
                table: "users",
                newName: "lockout_enabled");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                schema: "identity",
                table: "users",
                newName: "email_confirmed");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                schema: "identity",
                table: "users",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                schema: "identity",
                table: "users",
                newName: "access_failed_count");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "sessions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "sessions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SecurityVersion",
                schema: "identity",
                table: "sessions",
                newName: "security_version");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "identity",
                table: "sessions",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "identity",
                table: "sessions",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "sessions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_UserId",
                schema: "identity",
                table: "sessions",
                newName: "IX_sessions_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "identity",
                table: "roles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                schema: "identity",
                table: "roles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                schema: "identity",
                table: "roles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "identity",
                table: "user_tokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "identity",
                table: "user_tokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                schema: "identity",
                table: "user_tokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "user_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                schema: "identity",
                table: "user_roles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "user_roles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "user_roles",
                newName: "IX_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "user_profiles",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                schema: "identity",
                table: "user_profiles",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "FullName",
                schema: "identity",
                table: "user_profiles",
                newName: "full_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "user_profiles",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "user_profiles",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "user_logins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                schema: "identity",
                table: "user_logins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                schema: "identity",
                table: "user_logins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                schema: "identity",
                table: "user_logins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_UserLogins_UserId",
                schema: "identity",
                table: "user_logins",
                newName: "IX_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "user_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "user_claims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                schema: "identity",
                table: "user_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                schema: "identity",
                table: "user_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_UserClaims_UserId",
                schema: "identity",
                table: "user_claims",
                newName: "IX_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "role_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                schema: "identity",
                table: "role_claims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                schema: "identity",
                table: "role_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                schema: "identity",
                table: "role_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "identity",
                table: "role_claims",
                newName: "IX_role_claims_role_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "refresh_credentials",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UsedAt",
                schema: "identity",
                table: "refresh_credentials",
                newName: "used_at");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                schema: "identity",
                table: "refresh_credentials",
                newName: "token_hash");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                schema: "identity",
                table: "refresh_credentials",
                newName: "session_id");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                schema: "identity",
                table: "refresh_credentials",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "ReplacedByCredentialId",
                schema: "identity",
                table: "refresh_credentials",
                newName: "replaced_by_credential_id");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "identity",
                table: "refresh_credentials",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "refresh_credentials",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshCredentials_SessionId",
                schema: "identity",
                table: "refresh_credentials",
                newName: "IX_refresh_credentials_session_id");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshCredentials_ReplacedByCredentialId_SessionId",
                schema: "identity",
                table: "refresh_credentials",
                newName: "IX_refresh_credentials_replaced_by_credential_id_session_id");

            migrationBuilder.RenameColumn(
                name: "Submitted",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "submitted");

            migrationBuilder.RenameColumn(
                name: "Purpose",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "purpose");

            migrationBuilder.RenameColumn(
                name: "Email",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Closed",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "closed");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SecurityVersion",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "security_version");

            migrationBuilder.RenameColumn(
                name: "ResetUsed",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "reset_used");

            migrationBuilder.RenameColumn(
                name: "ResetTokenHash",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "reset_token_hash");

            migrationBuilder.RenameColumn(
                name: "ResetExpiresAt",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "reset_expires_at");

            migrationBuilder.RenameColumn(
                name: "KeyId",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "key_id");

            migrationBuilder.RenameColumn(
                name: "FailedAttempts",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "failed_attempts");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CodeMac",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "code_mac");

            migrationBuilder.RenameIndex(
                name: "IX_PublicOtpChallenges_UserId_Purpose_CreatedAt",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "IX_public_otp_challenges_user_id_purpose_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_PublicOtpChallenges_ResetTokenHash",
                schema: "identity",
                table: "public_otp_challenges",
                newName: "IX_public_otp_challenges_reset_token_hash");

            migrationBuilder.RenameColumn(
                name: "Count",
                schema: "identity",
                table: "email_verification_quotas",
                newName: "count");

            migrationBuilder.RenameColumn(
                name: "Key",
                schema: "identity",
                table: "email_verification_quotas",
                newName: "key");

            migrationBuilder.RenameColumn(
                name: "WindowStart",
                schema: "identity",
                table: "email_verification_quotas",
                newName: "window_start");

            migrationBuilder.RenameColumn(
                name: "WindowEnd",
                schema: "identity",
                table: "email_verification_quotas",
                newName: "window_end");

            migrationBuilder.RenameColumn(
                name: "State",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "Purpose",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "purpose");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "submitted_at");

            migrationBuilder.RenameColumn(
                name: "PendingLeaseUntil",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "pending_lease_until");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmailSnapshot",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "normalized_email_snapshot");

            migrationBuilder.RenameColumn(
                name: "LastAttemptAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "last_attempt_at");

            migrationBuilder.RenameColumn(
                name: "KeyId",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "key_id");

            migrationBuilder.RenameColumn(
                name: "InvalidatedAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "invalidated_at");

            migrationBuilder.RenameColumn(
                name: "FailedAttempts",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "failed_attempts");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "EmailSnapshot",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "email_snapshot");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConsumedAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "consumed_at");

            migrationBuilder.RenameColumn(
                name: "CodeMac",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "code_mac");

            migrationBuilder.RenameIndex(
                name: "IX_EmailVerificationChallenges_UserId_CreatedAt",
                schema: "identity",
                table: "email_verification_challenges",
                newName: "IX_email_verification_challenges_user_id_created_at");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sessions",
                schema: "identity",
                table: "sessions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                schema: "identity",
                table: "roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_tokens",
                schema: "identity",
                table: "user_tokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_roles",
                schema: "identity",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_profiles",
                schema: "identity",
                table: "user_profiles",
                column: "user_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_logins",
                schema: "identity",
                table: "user_logins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_claims",
                schema: "identity",
                table: "user_claims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_role_claims",
                schema: "identity",
                table: "role_claims",
                column: "id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_refresh_credentials_id_session_id",
                schema: "identity",
                table: "refresh_credentials",
                columns: new[] { "id", "session_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_credentials",
                schema: "identity",
                table: "refresh_credentials",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_public_otp_challenges",
                schema: "identity",
                table: "public_otp_challenges",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_email_verification_quotas",
                schema: "identity",
                table: "email_verification_quotas",
                column: "key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_email_verification_challenges",
                schema: "identity",
                table: "email_verification_challenges",
                column: "id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_SecurityVersion",
                schema: "identity",
                table: "users",
                sql: "\"security_version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Status",
                schema: "identity",
                table: "users",
                sql: "\"status\" IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Timestamps",
                schema: "identity",
                table: "users",
                sql: "\"updated_at\" >= \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_Expiry",
                schema: "identity",
                table: "sessions",
                sql: "\"expires_at\" > \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_Revocation",
                schema: "identity",
                table: "sessions",
                sql: "\"revoked_at\" IS NULL OR \"revoked_at\" >= \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_SecurityVersion",
                schema: "identity",
                table: "sessions",
                sql: "\"security_version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProfiles_Name",
                schema: "identity",
                table: "user_profiles",
                sql: "char_length(btrim(\"full_name\")) BETWEEN 2 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProfiles_Timestamps",
                schema: "identity",
                table: "user_profiles",
                sql: "\"updated_at\" >= \"created_at\"");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "refresh_credentials",
                column: "session_id",
                unique: true,
                filter: "\"used_at\" IS NULL AND \"revoked_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "refresh_credentials",
                column: "replaced_by_credential_id",
                unique: true,
                filter: "\"replaced_by_credential_id\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Expiry",
                schema: "identity",
                table: "refresh_credentials",
                sql: "\"expires_at\" > \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Hash",
                schema: "identity",
                table: "refresh_credentials",
                sql: "\"token_hash\" ~ '^[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Replacement",
                schema: "identity",
                table: "refresh_credentials",
                sql: "\"replaced_by_credential_id\" IS NULL OR (\"used_at\" IS NOT NULL AND \"replaced_by_credential_id\" <> \"id\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Revoked",
                schema: "identity",
                table: "refresh_credentials",
                sql: "\"revoked_at\" IS NULL OR \"revoked_at\" >= \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Used",
                schema: "identity",
                table: "refresh_credentials",
                sql: "\"used_at\" IS NULL OR \"used_at\" >= \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerificationQuota_Count",
                schema: "identity",
                table: "email_verification_quotas",
                sql: "\"count\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerificationQuota_Window",
                schema: "identity",
                table: "email_verification_quotas",
                sql: "\"window_end\" > \"window_start\"");

            migrationBuilder.CreateIndex(
                name: "UX_EmailVerificationChallenges_Active",
                schema: "identity",
                table: "email_verification_challenges",
                columns: new[] { "user_id", "purpose" },
                unique: true,
                filter: "\"state\" IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Attempts",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"failed_attempts\" BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Consumed",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "(\"state\" = 4) = (\"consumed_at\" IS NOT NULL) AND (\"consumed_at\" IS NULL OR (\"consumed_at\" >= \"created_at\" AND \"consumed_at\" < \"expires_at\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Invalidated",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "(\"state\" IN (3,5,6)) = (\"invalidated_at\" IS NOT NULL) AND (\"invalidated_at\" IS NULL OR \"invalidated_at\" >= \"created_at\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_KeyId",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"key_id\" ~ '^[A-Za-z0-9_-]{1,32}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_LastAttempt",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"last_attempt_at\" IS NULL OR \"last_attempt_at\" >= \"created_at\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Locked",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "(\"state\" = 6) = (\"failed_attempts\" = 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Mac",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"code_mac\" ~ '^[0-9A-F]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Purpose",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"purpose\" = 'EmailVerification'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_State",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"state\" BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Submitted",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "(\"state\" NOT IN (2,4,6) OR \"submitted_at\" IS NOT NULL) AND (\"submitted_at\" IS NULL OR (\"submitted_at\" >= \"created_at\" AND \"submitted_at\" < \"expires_at\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Time",
                schema: "identity",
                table: "email_verification_challenges",
                sql: "\"expires_at\" = \"created_at\" + interval '5 minutes' AND \"pending_lease_until\" = \"created_at\" + interval '2 minutes'");

            migrationBuilder.AddForeignKey(
                name: "FK_email_verification_challenges_users_user_id",
                schema: "identity",
                table: "email_verification_challenges",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_public_otp_challenges_users_user_id",
                schema: "identity",
                table: "public_otp_challenges",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_credentials_refresh_credentials_replaced_by_credent~",
                schema: "identity",
                table: "refresh_credentials",
                columns: new[] { "replaced_by_credential_id", "session_id" },
                principalSchema: "identity",
                principalTable: "refresh_credentials",
                principalColumns: new[] { "id", "session_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_credentials_sessions_session_id",
                schema: "identity",
                table: "refresh_credentials",
                column: "session_id",
                principalSchema: "identity",
                principalTable: "sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_role_claims_roles_role_id",
                schema: "identity",
                table: "role_claims",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_users_user_id",
                schema: "identity",
                table: "sessions",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_claims_users_user_id",
                schema: "identity",
                table: "user_claims",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_logins_users_user_id",
                schema: "identity",
                table: "user_logins",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_profiles_users_user_id",
                schema: "identity",
                table: "user_profiles",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_roles_role_id",
                schema: "identity",
                table: "user_roles",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_users_user_id",
                schema: "identity",
                table: "user_roles",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_tokens_users_user_id",
                schema: "identity",
                table: "user_tokens",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_verification_challenges_users_user_id",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_public_otp_challenges_users_user_id",
                schema: "identity",
                table: "public_otp_challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_credentials_refresh_credentials_replaced_by_credent~",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_credentials_sessions_session_id",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropForeignKey(
                name: "FK_role_claims_roles_role_id",
                schema: "identity",
                table: "role_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_users_user_id",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_user_claims_users_user_id",
                schema: "identity",
                table: "user_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_user_logins_users_user_id",
                schema: "identity",
                table: "user_logins");

            migrationBuilder.DropForeignKey(
                name: "FK_user_profiles_users_user_id",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_roles_role_id",
                schema: "identity",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_users_user_id",
                schema: "identity",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_tokens_users_user_id",
                schema: "identity",
                table: "user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_SecurityVersion",
                schema: "identity",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Status",
                schema: "identity",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Timestamps",
                schema: "identity",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sessions",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_Expiry",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_Revocation",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_SecurityVersion",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_tokens",
                schema: "identity",
                table: "user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_roles",
                schema: "identity",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_profiles",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProfiles_Name",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProfiles_Timestamps",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_logins",
                schema: "identity",
                table: "user_logins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_claims",
                schema: "identity",
                table: "user_claims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_role_claims",
                schema: "identity",
                table: "role_claims");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_refresh_credentials_id_session_id",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_credentials",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Expiry",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Hash",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Replacement",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Revoked",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshCredentials_Used",
                schema: "identity",
                table: "refresh_credentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_public_otp_challenges",
                schema: "identity",
                table: "public_otp_challenges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_email_verification_quotas",
                schema: "identity",
                table: "email_verification_quotas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerificationQuota_Count",
                schema: "identity",
                table: "email_verification_quotas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerificationQuota_Window",
                schema: "identity",
                table: "email_verification_quotas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_email_verification_challenges",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropIndex(
                name: "UX_EmailVerificationChallenges_Active",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Attempts",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Consumed",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Invalidated",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_KeyId",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_LastAttempt",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Locked",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Mac",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Purpose",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_State",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Submitted",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailVerification_Time",
                schema: "identity",
                table: "email_verification_challenges");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "identity",
                newName: "Users",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "sessions",
                schema: "identity",
                newName: "Sessions",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "identity",
                newName: "Roles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_tokens",
                schema: "identity",
                newName: "UserTokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_roles",
                schema: "identity",
                newName: "UserRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_profiles",
                schema: "identity",
                newName: "UserProfiles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_logins",
                schema: "identity",
                newName: "UserLogins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "user_claims",
                schema: "identity",
                newName: "UserClaims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "role_claims",
                schema: "identity",
                newName: "RoleClaims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "refresh_credentials",
                schema: "identity",
                newName: "RefreshCredentials",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "public_otp_challenges",
                schema: "identity",
                newName: "PublicOtpChallenges",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "email_verification_quotas",
                schema: "identity",
                newName: "EmailVerificationQuotas",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "email_verification_challenges",
                schema: "identity",
                newName: "EmailVerificationChallenges",
                newSchema: "identity");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "identity",
                table: "Users",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "email",
                schema: "identity",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                schema: "identity",
                table: "Users",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "identity",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "two_factor_enabled",
                schema: "identity",
                table: "Users",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "security_version",
                schema: "identity",
                table: "Users",
                newName: "SecurityVersion");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                schema: "identity",
                table: "Users",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "phone_number_confirmed",
                schema: "identity",
                table: "Users",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                schema: "identity",
                table: "Users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                schema: "identity",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "normalized_user_name",
                schema: "identity",
                table: "Users",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                schema: "identity",
                table: "Users",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "lockout_end",
                schema: "identity",
                table: "Users",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "lockout_enabled",
                schema: "identity",
                table: "Users",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "email_confirmed",
                schema: "identity",
                table: "Users",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                schema: "identity",
                table: "Users",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                schema: "identity",
                table: "Users",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "Sessions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "Sessions",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "security_version",
                schema: "identity",
                table: "Sessions",
                newName: "SecurityVersion");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                schema: "identity",
                table: "Sessions",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "identity",
                table: "Sessions",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "Sessions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_user_id",
                schema: "identity",
                table: "Sessions",
                newName: "IX_Sessions_UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "identity",
                table: "Roles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "Roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalized_name",
                schema: "identity",
                table: "Roles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                schema: "identity",
                table: "Roles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "value",
                schema: "identity",
                table: "UserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "identity",
                table: "UserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                schema: "identity",
                table: "UserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "UserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "role_id",
                schema: "identity",
                table: "UserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "UserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_user_roles_role_id",
                schema: "identity",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "identity",
                table: "UserProfiles",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                schema: "identity",
                table: "UserProfiles",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "full_name",
                schema: "identity",
                table: "UserProfiles",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "UserProfiles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "UserProfiles",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "UserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "provider_display_name",
                schema: "identity",
                table: "UserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "provider_key",
                schema: "identity",
                table: "UserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                schema: "identity",
                table: "UserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "IX_user_logins_user_id",
                schema: "identity",
                table: "UserLogins",
                newName: "IX_UserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "UserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "UserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                schema: "identity",
                table: "UserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                schema: "identity",
                table: "UserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "IX_user_claims_user_id",
                schema: "identity",
                table: "UserClaims",
                newName: "IX_UserClaims_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "RoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                schema: "identity",
                table: "RoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                schema: "identity",
                table: "RoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                schema: "identity",
                table: "RoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "IX_role_claims_role_id",
                schema: "identity",
                table: "RoleClaims",
                newName: "IX_RoleClaims_RoleId");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "used_at",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "UsedAt");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "TokenHash");

            migrationBuilder.RenameColumn(
                name: "session_id",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "replaced_by_credential_id",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "ReplacedByCredentialId");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_credentials_session_id",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "IX_RefreshCredentials_SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_credentials_replaced_by_credential_id_session_id",
                schema: "identity",
                table: "RefreshCredentials",
                newName: "IX_RefreshCredentials_ReplacedByCredentialId_SessionId");

            migrationBuilder.RenameColumn(
                name: "submitted",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "Submitted");

            migrationBuilder.RenameColumn(
                name: "purpose",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "Purpose");

            migrationBuilder.RenameColumn(
                name: "email",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "closed",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "Closed");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "security_version",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "SecurityVersion");

            migrationBuilder.RenameColumn(
                name: "reset_used",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "ResetUsed");

            migrationBuilder.RenameColumn(
                name: "reset_token_hash",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "ResetTokenHash");

            migrationBuilder.RenameColumn(
                name: "reset_expires_at",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "ResetExpiresAt");

            migrationBuilder.RenameColumn(
                name: "key_id",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "KeyId");

            migrationBuilder.RenameColumn(
                name: "failed_attempts",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "FailedAttempts");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "code_mac",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "CodeMac");

            migrationBuilder.RenameIndex(
                name: "IX_public_otp_challenges_user_id_purpose_created_at",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "IX_PublicOtpChallenges_UserId_Purpose_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_public_otp_challenges_reset_token_hash",
                schema: "identity",
                table: "PublicOtpChallenges",
                newName: "IX_PublicOtpChallenges_ResetTokenHash");

            migrationBuilder.RenameColumn(
                name: "count",
                schema: "identity",
                table: "EmailVerificationQuotas",
                newName: "Count");

            migrationBuilder.RenameColumn(
                name: "key",
                schema: "identity",
                table: "EmailVerificationQuotas",
                newName: "Key");

            migrationBuilder.RenameColumn(
                name: "window_start",
                schema: "identity",
                table: "EmailVerificationQuotas",
                newName: "WindowStart");

            migrationBuilder.RenameColumn(
                name: "window_end",
                schema: "identity",
                table: "EmailVerificationQuotas",
                newName: "WindowEnd");

            migrationBuilder.RenameColumn(
                name: "state",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "purpose",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "Purpose");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "submitted_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "pending_lease_until",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "PendingLeaseUntil");

            migrationBuilder.RenameColumn(
                name: "normalized_email_snapshot",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "NormalizedEmailSnapshot");

            migrationBuilder.RenameColumn(
                name: "last_attempt_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "LastAttemptAt");

            migrationBuilder.RenameColumn(
                name: "key_id",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "KeyId");

            migrationBuilder.RenameColumn(
                name: "invalidated_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "InvalidatedAt");

            migrationBuilder.RenameColumn(
                name: "failed_attempts",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "FailedAttempts");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "email_snapshot",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "EmailSnapshot");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "consumed_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "ConsumedAt");

            migrationBuilder.RenameColumn(
                name: "code_mac",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "CodeMac");

            migrationBuilder.RenameIndex(
                name: "IX_email_verification_challenges_user_id_created_at",
                schema: "identity",
                table: "EmailVerificationChallenges",
                newName: "IX_EmailVerificationChallenges_UserId_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                schema: "identity",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sessions",
                schema: "identity",
                table: "Sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                schema: "identity",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTokens",
                schema: "identity",
                table: "UserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                schema: "identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserProfiles",
                schema: "identity",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserLogins",
                schema: "identity",
                table: "UserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserClaims",
                schema: "identity",
                table: "UserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleClaims",
                schema: "identity",
                table: "RoleClaims",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RefreshCredentials_Id_SessionId",
                schema: "identity",
                table: "RefreshCredentials",
                columns: new[] { "Id", "SessionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshCredentials",
                schema: "identity",
                table: "RefreshCredentials",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PublicOtpChallenges",
                schema: "identity",
                table: "PublicOtpChallenges",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailVerificationQuotas",
                schema: "identity",
                table: "EmailVerificationQuotas",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailVerificationChallenges",
                schema: "identity",
                table: "EmailVerificationChallenges",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_SecurityVersion",
                schema: "identity",
                table: "Users",
                sql: "\"SecurityVersion\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Status",
                schema: "identity",
                table: "Users",
                sql: "\"Status\" IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Timestamps",
                schema: "identity",
                table: "Users",
                sql: "\"UpdatedAt\" >= \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_Expiry",
                schema: "identity",
                table: "Sessions",
                sql: "\"ExpiresAt\" > \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_Revocation",
                schema: "identity",
                table: "Sessions",
                sql: "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_SecurityVersion",
                schema: "identity",
                table: "Sessions",
                sql: "\"SecurityVersion\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProfiles_Name",
                schema: "identity",
                table: "UserProfiles",
                sql: "char_length(btrim(\"FullName\")) BETWEEN 2 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProfiles_Timestamps",
                schema: "identity",
                table: "UserProfiles",
                sql: "\"UpdatedAt\" >= \"CreatedAt\"");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_ActiveSession",
                schema: "identity",
                table: "RefreshCredentials",
                column: "SessionId",
                unique: true,
                filter: "\"UsedAt\" IS NULL AND \"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials",
                column: "ReplacedByCredentialId",
                unique: true,
                filter: "\"ReplacedByCredentialId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Expiry",
                schema: "identity",
                table: "RefreshCredentials",
                sql: "\"ExpiresAt\" > \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Hash",
                schema: "identity",
                table: "RefreshCredentials",
                sql: "\"TokenHash\" ~ '^[0-9a-f]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Replacement",
                schema: "identity",
                table: "RefreshCredentials",
                sql: "\"ReplacedByCredentialId\" IS NULL OR (\"UsedAt\" IS NOT NULL AND \"ReplacedByCredentialId\" <> \"Id\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Revoked",
                schema: "identity",
                table: "RefreshCredentials",
                sql: "\"RevokedAt\" IS NULL OR \"RevokedAt\" >= \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshCredentials_Used",
                schema: "identity",
                table: "RefreshCredentials",
                sql: "\"UsedAt\" IS NULL OR \"UsedAt\" >= \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerificationQuota_Count",
                schema: "identity",
                table: "EmailVerificationQuotas",
                sql: "\"Count\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerificationQuota_Window",
                schema: "identity",
                table: "EmailVerificationQuotas",
                sql: "\"WindowEnd\" > \"WindowStart\"");

            migrationBuilder.CreateIndex(
                name: "UX_EmailVerificationChallenges_Active",
                schema: "identity",
                table: "EmailVerificationChallenges",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "\"State\" IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Attempts",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"FailedAttempts\" BETWEEN 0 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Consumed",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "(\"State\" = 4) = (\"ConsumedAt\" IS NOT NULL) AND (\"ConsumedAt\" IS NULL OR (\"ConsumedAt\" >= \"CreatedAt\" AND \"ConsumedAt\" < \"ExpiresAt\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Invalidated",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "(\"State\" IN (3,5,6)) = (\"InvalidatedAt\" IS NOT NULL) AND (\"InvalidatedAt\" IS NULL OR \"InvalidatedAt\" >= \"CreatedAt\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_KeyId",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"KeyId\" ~ '^[A-Za-z0-9_-]{1,32}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_LastAttempt",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"LastAttemptAt\" IS NULL OR \"LastAttemptAt\" >= \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Locked",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "(\"State\" = 6) = (\"FailedAttempts\" = 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Mac",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"CodeMac\" ~ '^[0-9A-F]{64}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Purpose",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"Purpose\" = 'EmailVerification'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_State",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"State\" BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Submitted",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "(\"State\" NOT IN (2,4,6) OR \"SubmittedAt\" IS NOT NULL) AND (\"SubmittedAt\" IS NULL OR (\"SubmittedAt\" >= \"CreatedAt\" AND \"SubmittedAt\" < \"ExpiresAt\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailVerification_Time",
                schema: "identity",
                table: "EmailVerificationChallenges",
                sql: "\"ExpiresAt\" = \"CreatedAt\" + interval '5 minutes' AND \"PendingLeaseUntil\" = \"CreatedAt\" + interval '2 minutes'");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailVerificationChallenges_Users_UserId",
                schema: "identity",
                table: "EmailVerificationChallenges",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicOtpChallenges_Users_UserId",
                schema: "identity",
                table: "PublicOtpChallenges",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshCredentials_RefreshCredentials_ReplacedByCredentialI~",
                schema: "identity",
                table: "RefreshCredentials",
                columns: new[] { "ReplacedByCredentialId", "SessionId" },
                principalSchema: "identity",
                principalTable: "RefreshCredentials",
                principalColumns: new[] { "Id", "SessionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshCredentials_Sessions_SessionId",
                schema: "identity",
                table: "RefreshCredentials",
                column: "SessionId",
                principalSchema: "identity",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                schema: "identity",
                table: "RoleClaims",
                column: "RoleId",
                principalSchema: "identity",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Users_UserId",
                schema: "identity",
                table: "Sessions",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserClaims_Users_UserId",
                schema: "identity",
                table: "UserClaims",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLogins_Users_UserId",
                schema: "identity",
                table: "UserLogins",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Users_UserId",
                schema: "identity",
                table: "UserProfiles",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId",
                principalSchema: "identity",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "identity",
                table: "UserRoles",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTokens_Users_UserId",
                schema: "identity",
                table: "UserTokens",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
