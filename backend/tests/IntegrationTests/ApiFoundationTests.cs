using System.Net;
using System.Text.Json;
using HomeMatch.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace HomeMatch.IntegrationTests;

public class ApiFoundationTests
{
    [Fact]
    public async Task UnexpectedErrorUsesPublicCamelCaseEnvelopeAndRequestId()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");
        using var response = await client.GetAsync("/__tests/throw");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("https://frontend.example.test", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("private-database-detail", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.Matches("^[a-f0-9]{32}$", Assert.Single(response.Headers.GetValues("X-Request-Id")));

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal(new[] { "data", "error", "isSuccess", "timestamp" }, root.EnumerateObject().Select(p => p.Name).Order());
        Assert.False(root.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
        Assert.Equal(new[] { "code", "message" }, root.GetProperty("error").EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("Server.InternalError", root.GetProperty("error").GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("error").GetProperty("message").GetString()));
        Assert.Equal(DateTimeKind.Utc, root.GetProperty("timestamp").GetDateTime().Kind);
    }

    [Fact]
    public async Task RequestIdIsGeneratedPerRequestAndNotTakenFromCaller()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Request-Id", "caller-controlled-value");

        using var first = await client.GetAsync("/health");
        using var second = await client.GetAsync("/health");
        var firstId = Assert.Single(first.Headers.GetValues("X-Request-Id"));
        var secondId = Assert.Single(second.Headers.GetValues("X-Request-Id"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Matches("^[a-f0-9]{32}$", firstId);
        Assert.Matches("^[a-f0-9]{32}$", secondId);
        Assert.NotEqual(firstId, secondId);
    }

    [Fact]
    public async Task NoContentResponseRemainsEmpty()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/__tests/no-content");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.True(response.Headers.Contains("X-Request-Id"));
    }

    [Theory]
    [InlineData("https://frontend.example.test", true)]
    [InlineData("https://attacker.example.test", false)]
    [InlineData("https://frontend.example.test.attacker.test", false)]
    public async Task PreflightOnlyAllowsConfiguredOrigin(string origin, bool allowed)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/identity/ping");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type,Authorization,X-CSRF-TOKEN");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
            Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
            Assert.DoesNotContain("*", string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")));
        }
    }

    [Fact]
    public async Task AllowedOriginCanReadContractResponseHeaders()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "https://frontend.example.test");

        using var response = await client.GetAsync("/health");
        var exposed = string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers"));

        Assert.Contains("Retry-After", exposed);
        Assert.Contains("X-Request-Id", exposed);
        Assert.Contains("WWW-Authenticate", exposed);
    }

    [Theory]
    [InlineData("ConnectionStrings:DefaultConnection", "")]
    [InlineData("ConnectionStrings:DefaultConnection", "private-invalid-connection-marker")]
    [InlineData("Jwt:Secret", null)]
    [InlineData("Jwt:Secret", "private-invalid-secret-marker")]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", "")]
    [InlineData("Jwt:ExpirationMinutes", "1440")]
    [InlineData("Identity:Sessions:NormalLifetimeHours", "0")]
    [InlineData("Cors:AllowedOrigins:0", "*")]
    [InlineData("Cors:AllowedOrigins:0", "http://frontend.example.test")]
    [InlineData("Cors:AllowedOrigins:0", "https://frontend.example.test/")]
    [InlineData("Cors:AllowedOrigins:0", "https://frontend.example.test/path")]
    public void InvalidConfigurationFailsDuringHostStartup(string key, string? value)
    {
        using var factory = new ApiFactory(new Dictionary<string, string?> { [key] = value });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains(key.Split(':')[0] + ":", exception.Message);
        Assert.DoesNotContain("private-invalid-secret-marker", exception.ToString());
    }

    [Fact]
    public void DevelopmentAlsoRequiresAConfiguredSigningKey()
    {
        using var factory = new ApiFactory(
            new Dictionary<string, string?> { ["Jwt:Secret"] = null }, "Development");

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("Jwt:Secret", exception.Message);
    }

    [Theory]
    [InlineData("/scalar/v1")]
    [InlineData("/openapi/v1.json")]
    public async Task ApiDocumentationIsAvailableInDevelopmentOnly(string path)
    {
        await using var development = new ApiFactory(environment: "Development");
        await using var production = new ApiFactory();
        using var developmentClient = development.CreateClient();
        using var productionClient = production.CreateClient();

        using var developmentResponse = await developmentClient.GetAsync(path);
        using var productionResponse = await productionClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, developmentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, productionResponse.StatusCode);
    }
}
