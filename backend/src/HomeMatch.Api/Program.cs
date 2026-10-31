using HomeMatch.Api.Middleware;
using HomeMatch.Modules.Billing;
using HomeMatch.Modules.Identity;
using HomeMatch.Modules.Leasing;
using HomeMatch.Modules.Listings;
using HomeMatch.Modules.Messaging;
using HomeMatch.Modules.Notifications;
using HomeMatch.Modules.Properties;
using HomeMatch.Modules.Viewings;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services & OpenAPI (Native .NET 10)
builder.Services.AddOpenApi();

// 2. CORS configuration (allows Next.js frontend communication)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
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

// 4. HTTP Pipeline & Middlewares
app.UseTraceId();
app.UseCustomExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("HomeMatch Modular Monolith API")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseCors("AllowFrontend");

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
