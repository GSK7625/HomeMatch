using HomeMatch.Modules.Identity.Presentation.PublicOtp;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Infrastructure.EmailVerification;
using HomeMatch.Modules.Identity.Presentation.EmailVerification;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Application.Logout;
using HomeMatch.Modules.Identity.Presentation.Logout;
using HomeMatch.Modules.Identity.Presentation.Refresh;
using HomeMatch.Modules.Identity.Presentation.Login;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Passwords;
using HomeMatch.Modules.Identity.Presentation.Registration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HomeMatch.Modules.Identity.Presentation;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<IdentitySessionOptions>, IdentitySessionOptionsValidator>();
        services.AddSingleton<IValidateOptions<IdentityDatabaseOptions>, IdentityDatabaseOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<IdentitySessionOptions>()
            .Bind(configuration.GetSection(IdentitySessionOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<IdentityDatabaseOptions>()
            .Bind(configuration.GetSection(IdentityDatabaseOptions.SectionName))
            .ValidateOnStart();

        services.AddDbContext<IdentityDbContext>((provider, options) =>
        {
            var connection = provider.GetRequiredService<IOptions<IdentityDatabaseOptions>>().Value.DefaultConnection;
            options.UseNpgsql(connection, postgres =>
                postgres.MigrationsHistoryTable(IdentityDbContext.MigrationHistoryTable, IdentityDbContext.Schema));
        });
        services.AddIdentityCore<IdentityAccount>(options =>
        {
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version2;
            options.Stores.MaxLengthForKeys = 128;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = string.Empty;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
            options.Lockout.AllowedForNewUsers = false;
            // All password creation rules live in NewPasswordPolicy.
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
            .AddRoles<IdentityRole<long>>()
            .AddEntityFrameworkStores<IdentityDbContext>();
        services.RemoveAll<IUserStore<IdentityAccount>>();
        services.AddScoped<IUserStore<IdentityAccount>, SqlIdentityUserStore>();
        services.RemoveAll<IPasswordHasher<IdentityAccount>>();
        services.AddScoped<IPasswordHasher<IdentityAccount>, CompatiblePasswordHasher>();
        services.AddSingleton<INewPasswordPolicy, NewPasswordPolicy>();
        services.RemoveAll<IPasswordValidator<IdentityAccount>>();
        services.AddScoped<IPasswordValidator<IdentityAccount>, IdentityNewPasswordValidator>();
        services.AddScoped<IRegistrationStore, PostgresRegistrationStore>();
        services.AddScoped<AuthService>();
        services.AddScoped<IPublicOtpStore, PostgresPublicOtpStore>();
        services.AddScoped<SendPublicOtpHandler>();
        services.AddScoped<VerifyPublicOtpHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ILoginStore, PostgresLoginStore>();
        services.AddSingleton<LoginDummyCredential>();
        services.AddHostedService<LoginSecurityStartupCheck>();
        services.AddSingleton<IdentityQuotas>();
        services.AddScoped<RefreshCookie>();
        services.AddScoped<IRefreshSessionStore, PostgresRefreshSessionStore>();
        services.AddSingleton<IRefreshRequestQuota>(provider => provider.GetRequiredService<IdentityQuotas>());
        services.AddScoped<ILogoutSessionStore, PostgresLogoutSessionStore>();

        services.AddSingleton<IValidateOptions<EmailVerificationOptions>, EmailVerificationOptionsValidator>();
        services.AddOptions<EmailVerificationOptions>().Bind(configuration.GetSection(EmailVerificationOptions.SectionName))
            .Configure(value => value.Enabled = configuration.GetValue<bool>("Email:Enabled")).ValidateOnStart();
        services.AddSingleton<EmailVerificationCodeProtector>();
        services.AddScoped<IEmailVerificationStore, PostgresEmailVerificationStore>();
        services.AddScoped<IEmailVerificationDelivery, TransactionalVerificationDelivery>();
        services.AddScoped<RequestEmailVerificationHandler>();
        services.AddScoped<IAccessIdentityReader, DatabaseAccessIdentityReader>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddIdentityHttpSecurity();
        return services;
    }

    /// <summary>Explicit deployment/development operation; never migrates on every API startup.</summary>
    public static async Task InitializeIdentityDatabaseAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await EnsureIdentityBaselineAsync(database, cancellationToken);
        await database.Database.MigrateAsync(cancellationToken);
    }

    private static async Task EnsureIdentityBaselineAsync(
        IdentityDbContext database, CancellationToken cancellationToken)
    {
        const string baselineSql = """
            DO $baseline$
            BEGIN
                CREATE TABLE IF NOT EXISTS identity."__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL PRIMARY KEY,
                    "ProductVersion" character varying(32) NOT NULL
                );

                IF EXISTS (
                    SELECT 1 FROM information_schema.tables 
                    WHERE table_schema = 'identity' AND table_name = 'users'
                ) AND NOT EXISTS (
                    SELECT 1 FROM identity."__EFMigrationsHistory" 
                    WHERE "MigrationId" = '20261101031824_InitialCreate'
                ) THEN
                    ALTER TABLE identity.users 
                        ADD COLUMN IF NOT EXISTS user_name character varying(255),
                        ADD COLUMN IF NOT EXISTS normalized_user_name character varying(255),
                        ADD COLUMN IF NOT EXISTS normalized_email character varying(255),
                        ADD COLUMN IF NOT EXISTS email_confirmed boolean DEFAULT true NOT NULL,
                        ADD COLUMN IF NOT EXISTS security_stamp text DEFAULT md5(random()::text) NOT NULL,
                        ADD COLUMN IF NOT EXISTS concurrency_stamp text DEFAULT md5(random()::text) NOT NULL,
                        ADD COLUMN IF NOT EXISTS phone_number_confirmed boolean DEFAULT false NOT NULL,
                        ADD COLUMN IF NOT EXISTS two_factor_enabled boolean DEFAULT false NOT NULL,
                        ADD COLUMN IF NOT EXISTS lockout_end timestamp with time zone,
                        ADD COLUMN IF NOT EXISTS lockout_enabled boolean DEFAULT false NOT NULL,
                        ADD COLUMN IF NOT EXISTS access_failed_count integer DEFAULT 0 NOT NULL,
                        ADD COLUMN IF NOT EXISTS security_version bigint DEFAULT 1 NOT NULL;

                    UPDATE identity.users SET 
                        user_name = COALESCE(user_name, email), 
                        normalized_user_name = COALESCE(normalized_user_name, upper(email)), 
                        normalized_email = COALESCE(normalized_email, upper(email)),
                        security_stamp = COALESCE(security_stamp, md5(random()::text)),
                        concurrency_stamp = COALESCE(concurrency_stamp, md5(random()::text)),
                        security_version = COALESCE(security_version, 1);

                    ALTER TABLE identity.users 
                        ALTER COLUMN user_name SET NOT NULL,
                        ALTER COLUMN normalized_user_name SET NOT NULL,
                        ALTER COLUMN normalized_email SET NOT NULL;

                    CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON identity.users(normalized_user_name);
                    CREATE UNIQUE INDEX IF NOT EXISTS "UX_Users_NormalizedEmail" ON identity.users(normalized_email);

                    CREATE TABLE IF NOT EXISTS identity.email_verification_quotas (
                        key character varying(160) NOT NULL PRIMARY KEY,
                        window_start timestamp with time zone NOT NULL,
                        window_end timestamp with time zone NOT NULL,
                        count integer NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS identity.email_verification_challenges (
                        id uuid NOT NULL PRIMARY KEY,
                        user_id bigint NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
                        purpose character varying(32) NOT NULL,
                        email_snapshot character varying(254) NOT NULL,
                        normalized_email_snapshot character varying(254) NOT NULL,
                        code_mac character varying(64) NOT NULL,
                        key_id character varying(32) NOT NULL,
                        state integer NOT NULL,
                        created_at timestamp with time zone NOT NULL,
                        expires_at timestamp with time zone NOT NULL,
                        pending_lease_until timestamp with time zone NOT NULL,
                        submitted_at timestamp with time zone,
                        consumed_at timestamp with time zone,
                        invalidated_at timestamp with time zone,
                        failed_attempts integer NOT NULL,
                        last_attempt_at timestamp with time zone
                    );
                    CREATE INDEX IF NOT EXISTS "IX_email_verification_challenges_user_id_created_at" 
                        ON identity.email_verification_challenges(user_id, created_at);

                    CREATE TABLE IF NOT EXISTS identity.public_otp_challenges (
                        id uuid NOT NULL PRIMARY KEY,
                        user_id bigint NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
                        purpose character varying(32) NOT NULL,
                        email character varying(254) NOT NULL,
                        security_version bigint NOT NULL,
                        code_mac character varying(64) NOT NULL,
                        key_id character varying(32) NOT NULL,
                        created_at timestamp with time zone NOT NULL,
                        expires_at timestamp with time zone NOT NULL,
                        submitted boolean DEFAULT false NOT NULL,
                        closed boolean DEFAULT false NOT NULL,
                        failed_attempts integer DEFAULT 0 NOT NULL,
                        reset_token_hash character varying(64),
                        reset_expires_at timestamp with time zone,
                        reset_used boolean DEFAULT false NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS identity.sessions (
                        id uuid NOT NULL PRIMARY KEY,
                        user_id bigint NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
                        security_version bigint NOT NULL,
                        created_at timestamp with time zone NOT NULL,
                        expires_at timestamp with time zone NOT NULL,
                        revoked_at timestamp with time zone
                    );
                    CREATE INDEX IF NOT EXISTS "IX_sessions_user_id" 
                        ON identity.sessions(user_id);

                    CREATE TABLE IF NOT EXISTS identity.refresh_credential_metadata (
                        id bigint NOT NULL PRIMARY KEY REFERENCES identity.user_tokens(id) ON DELETE CASCADE,
                        session_id uuid NOT NULL REFERENCES identity.sessions(id) ON DELETE CASCADE,
                        used_at timestamp with time zone,
                        revoked_at timestamp with time zone,
                        replaced_by_credential_id bigint REFERENCES identity.refresh_credential_metadata(id)
                    );

                    INSERT INTO identity."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ('20261101031824_InitialCreate', '10.0.0')
                    ON CONFLICT ("MigrationId") DO NOTHING;
                END IF;
            END $baseline$;
            """;

        await database.Database.ExecuteSqlRawAsync(baselineSql, cancellationToken);
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapGet("/ping", () => Results.Ok(new { module = "Identity", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapAccountEndpoints();
        return endpoints;
    }
}
