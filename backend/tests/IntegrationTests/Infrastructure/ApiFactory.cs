using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests.Infrastructure;

internal sealed class ApiFactory(
    IReadOnlyDictionary<string, string?>? overrides = null,
    string environment = "Production") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Email:Enabled"] = "false",
                ["Jwt:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=55432;Database=homematch_host_tests;Username=homematch_test",
                ["Jwt:Issuer"] = "HomeMatch.Tests",
                ["Jwt:Audience"] = "HomeMatch.Tests.Client",
                ["Cors:AllowedOrigins:0"] = "https://frontend.example.test",
                ["Cors:AllowedOrigins:1"] = "https://frontend-alt.example.test"
            };

            if (overrides is not null)
            {
                foreach (var (key, value) in overrides)
                {
                    settings[key] = value;
                }
            }

            configuration.AddInMemoryCollection(settings);
        });
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ProbeStartupFilter>());
    }
}
