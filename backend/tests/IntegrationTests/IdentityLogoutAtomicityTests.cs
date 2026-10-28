using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.IntegrationTests;

public partial class IdentityLogoutTests
{
    [Fact]
    public async Task TwoConcurrentLogoutAllRequestsCannotBumpVersionTwice()
    {
        var account = await Account();
        await using var original = Factory();
        using var first = await Browser(original);
        var login = await Login(first, account);
        var barrier = new UserLockBarrier();
        await using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier))));
        using var client = await Browser(factory);
        SetRefresh(client, login.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        var responses = await Task.WhenAll(client.PostAsync("/api/v1/identity/auth/logout-all", null),
            client.PostAsync("/api/v1/identity/auth/logout-all", null));
        Assert.Equal(2, barrier.Arrivals);
        using var succeeded = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        using var denied = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        await AssertNoContent(succeeded, true);
        await AssertError(denied, HttpStatusCode.Unauthorized, "Identity.AuthenticationRequired");
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(2, (await db.Users.SingleAsync(value => value.Id == account.Id)).SecurityVersion);
    }

    [Theory]
    [InlineData("logout")]
    [InlineData("logout-all")]
    public async Task ConcurrentLogoutAndRefreshCannotLeaveAValidSuccessor(string route)
    {
        var account = await Account();
        await using var original = Factory();
        using var first = await Browser(original);
        var login = await Login(first, account);
        var barrier = new UserLockBarrier();
        await using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier))));
        using var client = await Browser(factory);
        SetRefresh(client, login.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Access);
        var requests = await Task.WhenAll(client.PostAsync("/api/v1/identity/auth/" + route, null),
            client.PostAsync("/api/v1/identity/auth/refresh", null));
        using var logout = requests[0];
        using var refresh = requests[1];
        await AssertNoContent(logout, true);
        Assert.Contains(refresh.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        if (refresh.StatusCode == HttpStatusCode.OK)
        {
            using var json = System.Text.Json.JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
            var access = json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
            await AssertMe(client, access, HttpStatusCode.Unauthorized);
        }
        await AssertMe(client, login.Access, HttpStatusCode.Unauthorized);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.All(await db.Sessions.Where(value => value.UserId == account.Id).ToListAsync(), value => Assert.NotNull(value.RevokedAt));
        Assert.All(await db.RefreshCredentials.Where(value => db.Sessions.Any(parent =>
            parent.Id == value.SessionId && parent.UserId == account.Id)).ToListAsync(), value => Assert.NotNull(value.RevokedAt));
    }

    [Fact]
    public async Task FailureAfterBulkRevocationRollsBackSessionsCredentialsAndSecurityVersion()
    {
        var account = await Account();
        await using var original = Factory();
        using var first = await Browser(original);
        using var second = await Browser(original);
        var firstLogin = await Login(first, account);
        var secondLogin = await Login(second, account);
        var failure = new FailUserUpdate();
        await using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(failure))));
        using var client = await Browser(factory);
        SetRefresh(client, firstLogin.Raw);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstLogin.Access);
        using var response = await client.PostAsync("/api/v1/identity/auth/logout-all", null);
        await AssertError(response, HttpStatusCode.InternalServerError, "Server.InternalError");
        Assert.Equal(2, failure.BulkUpdates);
        Assert.DoesNotContain("private-logout-rollback-marker", await response.Content.ReadAsStringAsync());
        await AssertMe(first, firstLogin.Access, HttpStatusCode.OK);
        await AssertMe(second, secondLogin.Access, HttpStatusCode.OK);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(1, (await db.Users.SingleAsync(value => value.Id == account.Id)).SecurityVersion);
        Assert.All(await db.Sessions.Where(value => value.UserId == account.Id).ToListAsync(), value => Assert.Null(value.RevokedAt));
        Assert.All(await db.RefreshCredentials.Where(value => db.Sessions.Any(parent =>
            parent.Id == value.SessionId && parent.UserId == account.Id)).ToListAsync(), value => Assert.Null(value.RevokedAt));
    }

    private sealed class UserLockBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => _arrivals;
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM identity.\"Users\"", StringComparison.Ordinal)
                && command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)
                && !command.CommandText.Contains("NormalizedEmail", StringComparison.Ordinal))
            {
                var number = Interlocked.Increment(ref _arrivals);
                if (number <= 2)
                {
                    if (number == 2) _ready.TrySetResult();
                    await _ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
                }
            }
            return result;
        }
    }

    private sealed class FailUserUpdate : DbCommandInterceptor
    {
        private int _bulkUpdates;
        public int BulkUpdates => _bulkUpdates;
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE identity.\"Sessions\"", StringComparison.Ordinal)
                || command.CommandText.StartsWith("UPDATE identity.\"RefreshCredentials\"", StringComparison.Ordinal))
                Interlocked.Increment(ref _bulkUpdates);
            if (command.CommandText.Contains("UPDATE identity.\"Users\"", StringComparison.Ordinal))
                throw new InvalidOperationException("private-logout-rollback-marker");
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDATE identity.\"Users\"", StringComparison.Ordinal))
                throw new InvalidOperationException("private-logout-rollback-marker");
            return ValueTask.FromResult(result);
        }
    }
}
