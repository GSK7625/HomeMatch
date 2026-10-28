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
        // TODO: Register application use cases and their adapters as each feature is implemented.
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
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            // The shared INewPasswordPolicy/Identity validator enforce scalar length and blocklist below.
            options.Password.RequiredLength = 15;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>();
        services.AddScoped<IdentityRoleSeeder>();
        services.AddSingleton<IPasswordBlocklist, OfflinePasswordBlocklist>();
        services.AddSingleton<INewPasswordPolicy, NewPasswordPolicy>();
        services.RemoveAll<IPasswordValidator<IdentityAccount>>();
        services.AddScoped<IPasswordValidator<IdentityAccount>, IdentityNewPasswordValidator>();
        services.AddHostedService<PasswordPolicyStartupCheck>();
        services.AddScoped<IRegistrationStore, PostgresRegistrationStore>();
        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<ILoginStore, PostgresLoginStore>();
        services.AddScoped<LoginHandler>();
        services.AddSingleton<LoginDummyCredential>();
        services.AddHostedService<LoginSecurityStartupCheck>();
        services.AddSingleton<LoginRequestReader>();
        services.AddSingleton<LoginRateLimiter>();
        services.AddScoped<RefreshCookie>();
        services.AddScoped<IRefreshSessionService, PostgresRefreshSessionService>();
        services.AddSingleton<IRefreshRequestQuota, RefreshRateLimiter>();
        services.AddSingleton<RefreshRequestReader>();
        services.AddScoped<ILogoutSessionService, PostgresLogoutSessionService>();
        services.AddSingleton<LogoutRequestReader>();
        services.AddSingleton<LogoutRateLimiter>();
        services.AddSingleton<RegistrationRequestReader>();
        services.AddSingleton<RegistrationRateLimiter>();

        services.AddSingleton<IValidateOptions<EmailVerificationOptions>, EmailVerificationOptionsValidator>();
        services.AddOptions<EmailVerificationOptions>().Bind(configuration.GetSection(EmailVerificationOptions.SectionName))
            .Configure(value => value.Enabled = configuration.GetValue<bool>("Email:Enabled")).ValidateOnStart();
        services.AddSingleton<EmailVerificationCodeProtector>();
        services.AddScoped<IEmailVerificationStore, PostgresEmailVerificationStore>();
        services.AddScoped<IEmailVerificationDelivery, TransactionalVerificationDelivery>();
        services.AddScoped<RequestEmailVerificationHandler>();
        services.AddSingleton<EmailVerificationRequestReader>();
        services.AddSingleton<EmailVerificationRateLimiter>();
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
        await scope.ServiceProvider.GetRequiredService<IdentityRoleSeeder>().SeedAsync(cancellationToken);
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapGet("/ping", () => Results.Ok(new { module = "Identity", status = "Healthy", timestamp = DateTime.UtcNow }));
        group.MapAccountEndpoints();
        return endpoints;
    }
}