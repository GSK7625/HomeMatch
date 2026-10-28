using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.Refresh;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace HomeMatch.IntegrationTests;

public partial class IdentityRefreshTests
{
    [Fact]
    public async Task ConcurrentRefreshAllowsOneRotationThenReplayRevokesWinnerToo()
    {
        var account = await Account();
        var barrier = new RefreshBarrier();
        await using var original = Factory();
        await using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<IdentityDbContext>(options => options.AddInterceptors(barrier))));
        using var client = await Browser(factory);
        using var login = await Login(client, account);
        SetRefresh(client, Raw(login));
        var responses = await Task.WhenAll(client.PostAsync("/api/v1/identity/auth/refresh", null),
            client.PostAsync("/api/v1/identity/auth/refresh", null));
        Assert.Equal(2, barrier.Arrivals);
        using var winner = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        using var loser = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        await AssertFailure(loser, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
        var winnerAccess = await Access(winner);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", winnerAccess);
        using var me = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        SetRefresh(client, Raw(winner));
        using var retry = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(retry, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var session = await db.Sessions.SingleAsync(value => value.UserId == account.Id);
        var history = await db.RefreshCredentials.Where(value => value.SessionId == session.Id).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.Single(history, value => value.UsedAt is not null);
        Assert.All(history, value => Assert.NotNull(value.RevokedAt));
        Assert.NotNull(session.RevokedAt);
    }

    [Fact]
    public async Task FailedIssuanceRollsBackConsumptionReplacementAndNewCredential()
    {
        var account = await Account();
        await using var original = Factory();
        using var initialClient = await Browser(original);
        using var login = await Login(initialClient, account);
        var raw = Raw(login);
        var access = await Access(login);
        await using var failing = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccessTokenIssuer>();
            services.AddSingleton<IAccessTokenIssuer, RefreshThrowingIssuer>();
        }));
        using var client = await Browser(failing);
        SetRefresh(client, raw);
        using var failed = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(failed, HttpStatusCode.InternalServerError, "Server.InternalError");
        Assert.DoesNotContain("private-refresh-issuance-marker", await failed.Content.ReadAsStringAsync());
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var session = await db.Sessions.SingleAsync(value => value.UserId == account.Id);
            var credential = await db.RefreshCredentials.SingleAsync(value => value.SessionId == session.Id);
            Assert.Null(credential.UsedAt);
            Assert.Null(credential.ReplacedByCredentialId);
            Assert.Null(credential.RevokedAt);
            Assert.Null(session.RevokedAt);
        }
        initialClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        SetRefresh(initialClient, raw);
        using var stillWorks = await initialClient.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    [Fact]
    public async Task DatabasePreventsTwoUnusedCredentialsInSameSession()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var login = await Login(client, account);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var session = await db.Sessions.SingleAsync(value => value.UserId == account.Id);
        db.RefreshCredentials.Add(new HomeMatch.Modules.Identity.Domain.Entities.RefreshCredential(
            session.Id, RefreshSecrets.Create().Hash, DateTimeOffset.UtcNow, session.ExpiresAt));
        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("UX_RefreshCredentials_ActiveSession", postgres.ConstraintName);
    }

    [Fact]
    public async Task DatabaseRejectsReplacementFromAnotherFamilyAndCascadeDeletesWholeChain()
    {
        var account = await Account();
        await using var factory = Factory();
        using var client = await Browser(factory);
        using var first = await Login(client, account);
        var firstRaw = Raw(first);
        using var second = await Login(client, account);
        var secondRaw = Raw(second);
        SetRefresh(client, firstRaw);
        using var rotated = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var firstHash = RefreshSecrets.TryHash(firstRaw)!;
        var secondHash = RefreshSecrets.TryHash(secondRaw)!;
        var old = await db.RefreshCredentials.AsNoTracking().SingleAsync(value => value.TokenHash == firstHash);
        var unrelated = await db.RefreshCredentials.AsNoTracking().SingleAsync(value => value.TokenHash == secondHash);
        var failure = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE identity."RefreshCredentials" SET "ReplacedByCredentialId" = {unrelated.Id} WHERE "Id" = {old.Id}
            """));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, failure.SqlState);
        await db.Users.Where(value => value.Id == account.Id).ExecuteDeleteAsync();
        Assert.False(await db.Sessions.AnyAsync(value => value.UserId == account.Id));
        Assert.False(await db.RefreshCredentials.AnyAsync(value => value.SessionId == old.SessionId || value.SessionId == unrelated.SessionId));
    }

    [Fact]
    public async Task DevHttpRotationAndInvalidCookieDeletionUseSameDevCookieName()
    {
        var account = await Account();
        await using var factory = Factory(environment: "Development");
        using var client = await Browser(factory, "http://localhost");
        using var login = await Login(client, account);
        SetRefresh(client, Raw(login), "HM.Refresh.Dev");
        using var rotated = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        Assert.StartsWith("HM.Refresh.Dev=", Assert.Single(rotated.Headers.GetValues("Set-Cookie")));
        using var replay = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(replay, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
        var header = Assert.Single(replay.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
        Assert.StartsWith("hm.refresh.dev=", header);
        Assert.DoesNotContain("; secure", header);
    }

    [Fact]
    public async Task ExpiryDuringIssuanceReturns401AndRollsBackRotation()
    {
        var account = await Account();
        await using var original = Factory();
        using var firstClient = await Browser(original);
        using var login = await Login(firstClient, account);
        var raw = Raw(login);
        await using var expiring = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccessTokenIssuer>();
            services.AddSingleton<IAccessTokenIssuer, ExpiringIssuer>();
        }));
        using var client = await Browser(expiring);
        SetRefresh(client, raw);
        using var response = await client.PostAsync("/api/v1/identity/auth/refresh", null);
        await AssertFailure(response, HttpStatusCode.Unauthorized, "Identity.SessionInvalid", true);
        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var session = await db.Sessions.SingleAsync(value => value.UserId == account.Id);
        var credential = await db.RefreshCredentials.SingleAsync(value => value.SessionId == session.Id);
        Assert.Null(credential.UsedAt);
        Assert.Null(credential.ReplacedByCredentialId);
    }

    private sealed class ExpiringIssuer : IAccessTokenIssuer
    {
        public IssuedAccessToken Issue(AccessTokenSubject subject) => throw new AccessTokenSessionExpiredException();
    }

    private sealed class RefreshThrowingIssuer : IAccessTokenIssuer
    {
        public IssuedAccessToken Issue(AccessTokenSubject subject) => throw new InvalidOperationException("private-refresh-issuance-marker");
    }

    private sealed class RefreshBarrier : DbCommandInterceptor
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
}
