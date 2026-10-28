using System.Threading.RateLimiting;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Configuration;
using HomeMatch.Modules.Identity.Presentation.Authentication;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomeMatch.Modules.Identity.Presentation;

public static class IdentityHttpSecurityExtensions
{
    public static IServiceCollection AddIdentityHttpSecurity(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentIdentity, HttpCurrentIdentity>();
        services.AddScoped<IAccessIdentityReader, DatabaseAccessIdentityReader>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IdentityJwtEvents>();
        services.AddScoped<IdentityBrowserSecurity>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, configured) =>
            {
                var jwt = configured.Value;
                bearer.MapInboundClaims = false;
                bearer.SaveToken = false;
                bearer.IncludeErrorDetails = false;
                bearer.EventsType = typeof(IdentityJwtEvents);
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    RequireSignedTokens = true, ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.Secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256], ValidTypes = ["JWT"],
                    ValidateIssuer = true, ValidIssuer = jwt.Issuer,
                    ValidateAudience = true, ValidAudience = jwt.Audience,
                    RequireExpirationTime = true, ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                    NameClaimType = "sub", RoleClaimType = "role"
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(IdentityPolicies.Authenticated, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser());
            foreach (var role in AccountRoles.All)
                options.AddPolicy("Identity." + role, policy => policy
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser().RequireRole(role));
        });
        services.AddAntiforgery();
        services.AddOptions<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>()
            .Configure<IHostEnvironment>((options, environment) =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = environment.IsDevelopment() ? "HM.Antiforgery.Dev" : "__Host-HM.Antiforgery";
                options.Cookie.Path = "/";
                options.Cookie.Domain = null;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("Identity.Csrf", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60, Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0, AutoReplenishment = true
                }));
            options.AddPolicy("Identity.Profile", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst("sub")!.Value,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120, Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0, AutoReplenishment = true
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Failure(new Error("Identity.RateLimitExceeded",
                        "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.")), cancellationToken);
            };
        });
        return services;
    }

    public static IApplicationBuilder UseIdentityHttpSecurity(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1/identity"))
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = "no-store";
                    return Task.CompletedTask;
                });
            await next(context);
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        return app;
    }
}
