using HomeMatch.Api.Middleware;
using HomeMatch.Modules.Billing;
using HomeMatch.Modules.Identity;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Modules.Identity.Infrastructure.Persistence.Seed;
using HomeMatch.Modules.Leasing;
using HomeMatch.Modules.Listings;
using HomeMatch.Modules.Messaging;
using HomeMatch.Modules.Notifications;
using HomeMatch.Modules.Properties;
using HomeMatch.Modules.Viewings;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services & OpenAPI (Native .NET 10 có JWT Bearer Authorize)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Dán Access Token vào đây (không cần gõ chữ 'Bearer '):"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        // Chuẩn hóa Type cho các Query Parameter để Swagger UI hiển thị đúng integer($int64)
        if (document.Paths != null)
        {
            foreach (var path in document.Paths.Values)
            {
                if (path.Operations != null)
                {
                    foreach (var operation in path.Operations.Values)
                    {
                        if (operation.Parameters != null)
                        {
                            foreach (var param in operation.Parameters)
                            {
                                if (param.Schema is OpenApiSchema schema && schema.Type.HasValue)
                                {
                                    if ((schema.Type.Value & JsonSchemaType.Integer) != 0)
                                    {
                                        schema.Type = JsonSchemaType.Integer;
                                    }
                                    else if ((schema.Type.Value & JsonSchemaType.String) != 0)
                                    {
                                        schema.Type = JsonSchemaType.String;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return Task.CompletedTask;
    });
});

// 2. CORS configuration (reads allowed origins dynamically from appsettings / environment)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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
    // Native .NET 10 OpenAPI document endpoint (/openapi/v1.json)
    app.MapOpenApi();

    // Swagger UI visual client rendering from /openapi/v1.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "HomeMatch API v1");
        options.RoutePrefix = "swagger";
    });

    // Tự động chạy Migration và Seed dữ liệu khi khởi động
    using var scope = app.Services.CreateScope();
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDb.Database.MigrateAsync();
    await IdentityDataSeeder.SeedRolesAsync(scope.ServiceProvider);
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

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
