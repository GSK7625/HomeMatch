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
using HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetTenantDashboard;
using HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetViewedListings;
using HomeMatch.Modules.Identity.Application.Interfaces;
using HomeMatch.Modules.Identity.Infrastructure.Identity;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Modules.Identity.Infrastructure.Persistence.Repositories;
using HomeMatch.Modules.Identity.Infrastructure.Services;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Auth;
using HomeMatch.Modules.Identity.Presentation.Endpoints.Profile;
using HomeMatch.Modules.Identity.Presentation.Security;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeMatch.Modules.Identity;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
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

            // Hỗ trợ xác thực JWT qua query string (?access_token=...) cho kết nối WebSocket / SignalR Hub
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken)
                        && (path.StartsWithSegments("/api/v1/messaging/hub") || path.StartsWithSegments("/hubs")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        services.AddIdentityRateLimiting();

        // 4. Register Infrastructure Services & Repositories
        services.Configure<EmailSettings>(options =>
        {
            configuration.GetSection(EmailSettings.SectionName).Bind(options);

            // Tự động map từ Email:Smtp hoặc biến môi trường nếu EmailSettings chưa có
            if (string.IsNullOrWhiteSpace(options.SenderEmail))
            {
                options.SenderEmail = configuration["Email:Smtp:FromAddress"]
                    ?? configuration["Email:Smtp:Username"]
                    ?? configuration["EMAIL_SENDER_EMAIL"]
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.AppPassword))
            {
                options.AppPassword = configuration["Email:Smtp:AppPassword"]
                    ?? configuration["EMAIL_APP_PASSWORD"]
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.SenderName) || options.SenderName == "HomeMatch Support")
            {
                var fromName = configuration["Email:Smtp:FromName"];
                if (!string.IsNullOrWhiteSpace(fromName))
                {
                    options.SenderName = fromName;
                }
            }

            var frontendUrl = configuration["FRONTEND_URL"]
                ?? configuration["FrontendUrl"]
                ?? configuration["AppUrl"]
                ?? configuration["EmailSettings:FrontendUrl"];
            if (!string.IsNullOrWhiteSpace(frontendUrl))
            {
                options.FrontendUrl = frontendUrl.TrimEnd('/');
            }
        });
        services.AddScoped<IIdentityAccountService, IdentityAccountService>();
        services.AddScoped<IIdentityUserReader, IdentityUserReader>();
        services.TryAddScoped<ITenantListingActivityReader, FallbackTenantListingActivityReader>();
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

        // Mục 3.4: Tenant Overview & Dashboard Handlers
        services.AddScoped<GetTenantDashboardHandler>();
        services.AddScoped<GetViewedListingsHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").WithTags("Identity");
        group.MapGet("/ping", () => Results.Ok(new { module = "Identity", status = "Healthy", timestamp = DateTime.UtcNow }));

        // Map Auth Endpoints (UC-A01 đến UC-A10)
        group.MapAuthEndpoints();

        // Map Profile Endpoints (UC-A11: /me) & Tenant Overview (Mục 3.4.1 & 3.4.2)
        group.MapProfileEndpoints();
        group.MapTenantOverviewEndpoints();

        return endpoints;
    }
}
