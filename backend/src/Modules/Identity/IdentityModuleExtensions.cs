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
        await database.Database.MigrateAsync(cancellationToken);
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapGet("/ping", () => Results.Ok(new { module = "Identity", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapAccountEndpoints();
        return endpoints;
    }
}
