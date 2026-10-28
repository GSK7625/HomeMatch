using System.Text.Json;
using HomeMatch.Api.Configuration;
using HomeMatch.Api.Middleware;
using HomeMatch.Modules.Billing;
using HomeMatch.Modules.Identity;
using HomeMatch.Modules.Identity.Presentation;
using HomeMatch.Modules.Leasing;
using HomeMatch.Modules.Listings;
using HomeMatch.Modules.Messaging;
using HomeMatch.Modules.Notifications;
using HomeMatch.Modules.Properties;
using HomeMatch.Modules.Viewings;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services & OpenAPI (Native .NET 10)
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// 2. CORS configuration (allows Next.js frontend communication)
builder.Services.AddSingleton<IValidateOptions<FrontendCorsOptions>, FrontendCorsOptionsValidator>();
builder.Services.AddOptions<FrontendCorsOptions>()
    .Bind(builder.Configuration.GetSection(FrontendCorsOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddCors();
builder.Services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
    .Configure<IOptions<FrontendCorsOptions>>((options, frontend) =>
    {
        options.AddPolicy(FrontendCorsOptions.PolicyName, policy =>
        {
            policy.WithOrigins(frontend.Value.AllowedOrigins)
                .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN")
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithExposedHeaders("Retry-After", "X-Request-Id", "WWW-Authenticate")
                .AllowCredentials();
        });
    });

// 3. Register Modules
builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddPropertiesModule(builder.Configuration)
    .AddListingsModule(builder.Configuration)
    .AddViewingsModule(builder.Configuration)
    .AddMessagingModule(builder.Configuration)
    .AddLeasingModule(builder.Configuration)
    .AddBillingModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration);

var app = builder.Build();

if (args.Contains("--initialize-identity", StringComparer.Ordinal))
{
    await app.Services.InitializeIdentityDatabaseAsync();
    return;
}

// 4. HTTP Pipeline & Middlewares
app.UseMiddleware<RequestIdMiddleware>();
app.UseCustomExceptionHandling();
app.UseCors(FrontendCorsOptions.PolicyName);
app.UseIdentityHttpSecurity();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("HomeMatch Modular Monolith API")
               .WithTheme(ScalarTheme.Moon);
    });
}

// 5. System Health & Status Endpoints
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    framework = ".NET 10"
})).WithTags("System");

app.MapGet("/api/v1/system/status", () => Results.Ok(new
{
    application = "HomeMatch Modular Monolith API",
    version = "1.0.0",
    framework = ".NET 10",
    modules = new[]
    {
        "Identity", "Properties", "Listings", "Viewings",
        "Messaging", "Leasing", "Billing", "Notifications"
    },
    status = "Online",
    serverTime = DateTime.UtcNow
})).WithTags("System");

// 6. Map Module Endpoints
app.MapIdentityEndpoints();
app.MapPropertiesEndpoints();
app.MapListingsEndpoints();
app.MapViewingsEndpoints();
app.MapMessagingEndpoints();
app.MapLeasingEndpoints();
app.MapBillingEndpoints();
app.MapNotificationsEndpoints();

app.Run();

// Expose the entry point to WebApplicationFactory without adding production-only test routes.
public partial class Program { }
