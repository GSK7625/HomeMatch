using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using HomeMatch.Modules.Identity.Application.Features.Auth.ConfirmEmail;
using HomeMatch.Modules.Identity.Application.Features.Auth.ForgotPassword;
using HomeMatch.Modules.Identity.Application.Features.Auth.Login;
using HomeMatch.Modules.Identity.Application.Features.Auth.Logout;
using HomeMatch.Modules.Identity.Application.Features.Auth.LogoutAll;
using HomeMatch.Modules.Identity.Application.Features.Auth.RefreshToken;
using HomeMatch.Modules.Identity.Application.Features.Auth.Register;
using HomeMatch.Modules.Identity.Application.Features.Auth.ResendConfirmation;
using HomeMatch.Modules.Identity.Application.Features.Auth.ResetPassword;
using HomeMatch.Modules.Identity.Application.Features.Profile.GetCurrentUser;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Infrastructure.Identity;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Modules.Identity.Infrastructure.Persistence.Repositories;
using HomeMatch.Modules.Identity.Infrastructure.Services;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Auth;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Profile;
using HomeMatch.Modules.Identity.Presentation.Security;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 0. Kích hoạt .NET 10 Minimal API Validation (Data Annotations via Source Generator)
        services.AddValidation();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // 1. Configure EF Core DbContext with PostgreSQL
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema);
            });
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // 2. Configure ASP.NET Core Identity Core
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            // Password policy (BR-06): 8-128 chars, requires letters and numbers
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;

            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddRoles<ApplicationRole>()
        .AddRoleManager<RoleManager<ApplicationRole>>()
        .AddUserManager<UserManager<ApplicationUser>>()
        .AddSignInManager()
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        // 3. Configure JWT Authentication & Authorization
        var jwtSecret = configuration["Jwt:Secret"] ?? "super_secret_jwt_key_with_at_least_32_characters_long_123!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "HomeMatch";
        var jwtAudience = configuration["Jwt:Audience"] ?? "HomeMatchApp";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();
        services.AddIdentityRateLimiting();

        // 4. Register Infrastructure Services & Repositories
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IIdentityAccountService, IdentityAccountService>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // 5. Register Use Case Handlers
        // Phase 2 Handlers
        services.AddScoped<RegisterHandler>();
        services.AddScoped<ConfirmEmailHandler>();
        services.AddScoped<ResendConfirmationHandler>();

        // Phase 3 Handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<LogoutAllHandler>();
        services.AddScoped<GetCurrentUserHandler>();

        // Phase 4 Handlers
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();

        // 6. Cross-Module Contracts (HomeMatch.Shared/Contracts)
        services.AddScoped<IIdentityUserReader, IdentityUserReader>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapGet("/ping", () => Results.Ok(new { module = "Identity", status = "Healthy", timestamp = DateTime.UtcNow }));

        // Map Auth Endpoints (UC-A01 đến UC-A10)
        group.MapAuthEndpoints();

        // Map Profile Endpoints (UC-A11: /me)
        group.MapProfileEndpoints();

        return endpoints;
    }
}
