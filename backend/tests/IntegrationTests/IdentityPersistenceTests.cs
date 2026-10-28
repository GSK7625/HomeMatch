using HomeMatch.IntegrationTests.Infrastructure;
using HomeMatch.Modules.Identity;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HomeMatch.IntegrationTests;

public class IdentityPersistenceTests(PostgresDatabaseFixture database)
    : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Mat khau integration test 2026!";

    [Fact]
    public async Task MigrationHistoryAndTablesStayInsideIdentitySchema()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(5, applied.Length);
        Assert.EndsWith("_InitialIdentity", applied[0]);
        Assert.EndsWith("_AddAccountSessions", applied[1]);
        Assert.EndsWith("_AddRefreshCredentials", applied[2]);
        Assert.EndsWith("_AddRefreshRotation", applied[3]);
        Assert.EndsWith("_AddEmailVerification", applied[4]);
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.All(context.Model.GetEntityTypes(), entity => Assert.Equal("identity", entity.GetSchema()));

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT count(*) FROM t04_neighbor."Sentinel" WHERE "Value" = 42),
                   (SELECT count(*) FROM information_schema.tables
                    WHERE table_schema = 'identity' AND table_name = '__EFMigrationsHistory'),
                   (SELECT count(*) FROM information_schema.tables
                    WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory')
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal(1L, reader.GetInt64(1));
        Assert.Equal(0L, reader.GetInt64(2));
    }

    [Fact]
    public async Task InitializerCanRunRepeatedlyAndConcurrentlyWithoutDuplicatingRoles()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var usersBefore = await context.Users.CountAsync();

        await database.Services.InitializeIdentityDatabaseAsync();
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => database.Services.InitializeIdentityDatabaseAsync()));

        var roles = await context.Roles.AsNoTracking().ToListAsync();
        Assert.Equal(AccountRoles.All.Order(), roles.Select(role => role.Name!).Order());
        Assert.Equal(3, roles.Select(role => role.Id).Distinct().Count());
        Assert.Equal(usersBefore, await context.Users.CountAsync());
    }

    [Fact]
    public async Task ExplicitInitializationCommandExitsSuccessfullyWithoutStartingHttpServer()
    {
        var start = new System.Diagnostics.ProcessStartInfo("dotnet")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--initialize-identity");
        start.Environment["ConnectionStrings__DefaultConnection"] = database.ConnectionString;
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        // Migration initialization needs database options, not HTTP/JWT configuration.
        start.Environment["Jwt__Secret"] = string.Empty;
        start.Environment["Logging__LogLevel__Default"] = "None";
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45));
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        // Drain both streams without exposing connection strings in assertions.
        await Task.WhenAll(output, error);
        Assert.Equal(0, process.ExitCode);
        Assert.DoesNotContain("Now listening on", await output, StringComparison.OrdinalIgnoreCase);
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(3, await context.Roles.CountAsync());
    }

    [Fact]
    public async Task CredentialProfileAndRoleCommitTogetherWithoutConfirmingEmail()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = NewAccount();
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            AssertSuccess(await manager.CreateAsync(account, Password));
            AssertSuccess(await manager.AddToRoleAsync(account, AccountRoles.Tenant));
            context.Profiles.Add(new UserProfile(account.Id, "  Nguyễn Văn An  ", account.CreatedAt));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        context.ChangeTracker.Clear();
        var stored = await context.Users.SingleAsync(user => user.Id == account.Id);
        var profile = await context.Profiles.SingleAsync(value => value.UserId == account.Id);
        Assert.Equal(AccountStatus.Active, stored.Status);
        Assert.False(stored.EmailConfirmed);
        Assert.Equal(1L, stored.SecurityVersion);
        Assert.True(stored.LockoutEnabled);
        Assert.Equal(stored.Email!.ToUpperInvariant(), stored.NormalizedEmail);
        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.True(await manager.CheckPasswordAsync(stored, Password));
        Assert.False(await manager.CheckPasswordAsync(stored, "incorrect password for test"));
        Assert.Equal("Nguyễn Văn An", profile.FullName);
        Assert.Equal(new[] { AccountRoles.Tenant }, await manager.GetRolesAsync(stored));
    }

    [Fact]
    public async Task DatabaseUniquenessPreventsTwoConcurrentNormalizedEmails()
    {
        var email = $"race-{Guid.NewGuid():N}@example.test";
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<string?> InsertAsync(string input)
        {
            await using var scope = database.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var account = new IdentityAccount(input, DateTimeOffset.UtcNow)
            {
                // Different usernames ensure this test exercises the email index specifically.
                UserName = Guid.NewGuid().ToString("N"),
                NormalizedUserName = Guid.NewGuid().ToString("N"),
                NormalizedEmail = input.ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            account.PasswordHash = new PasswordHasher<IdentityAccount>().HashPassword(account, Password);
            context.Users.Add(account);
            await gate.Task;
            try
            {
                await context.SaveChangesAsync();
                return null;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
                                                     && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return postgres.ConstraintName;
            }
        }

        var first = InsertAsync(email);
        var second = InsertAsync(email.ToUpperInvariant());
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result == "UX_Users_NormalizedEmail");
        await using var verification = database.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(user => user.NormalizedEmail == email.ToUpperInvariant()));
    }

    [Fact]
    public async Task RollbackRemovesCredentialProfileAndRoleAssignment()
    {
        var account = NewAccount();
        await using (var scope = database.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
            await using var transaction = await context.Database.BeginTransactionAsync();
            AssertSuccess(await manager.CreateAsync(account, Password));
            AssertSuccess(await manager.AddToRoleAsync(account, AccountRoles.Landlord));
            context.Profiles.Add(new UserProfile(account.Id, "Nguyễn An", account.CreatedAt));
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verify = database.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.False(await db.Users.AnyAsync(user => user.Id == account.Id));
        Assert.False(await db.Profiles.AnyAsync(profile => profile.UserId == account.Id));
        Assert.False(await db.UserRoles.AnyAsync(role => role.UserId == account.Id));
    }

    [Fact]
    public async Task SecondBusinessRoleIsRejectedByDatabase()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = NewAccount();
        AssertSuccess(await manager.CreateAsync(account, Password));
        AssertSuccess(await manager.AddToRoleAsync(account, AccountRoles.Tenant));

        var error = await Assert.ThrowsAsync<DbUpdateException>(
            () => manager.AddToRoleAsync(account, AccountRoles.Landlord));

        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("UX_UserRoles_UserId", postgres.ConstraintName);
    }

    [Fact]
    public async Task ConcurrentUserUpdatesReturnIdentityConcurrencyFailure()
    {
        var account = NewAccount();
        await using var first = database.Services.CreateAsyncScope();
        await using var second = database.Services.CreateAsyncScope();
        var firstManager = first.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var secondManager = second.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        AssertSuccess(await firstManager.CreateAsync(account, Password));
        var stale = await secondManager.FindByIdAsync(account.Id.ToString());
        Assert.NotNull(stale);

        account.PhoneNumber = "+84912345678";
        AssertSuccess(await firstManager.UpdateAsync(account));
        stale.PhoneNumber = "+84987654321";
        var result = await secondManager.UpdateAsync(stale);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "ConcurrencyFailure");
    }

    [Fact]
    public async Task OrphanProfileIsRejectedByForeignKey()
    {
        await using var scope = database.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        context.Profiles.Add(new UserProfile(Guid.NewGuid(), "Nguyễn An", DateTimeOffset.UtcNow));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidAccountStateOrSecurityVersionCannotBypassDatabaseChecks(bool invalidStatus)
    {
        await using var scope = database.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = NewAccount();
        if (invalidStatus) account.Status = (AccountStatus)0;
        else account.SecurityVersion = 0;

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => manager.CreateAsync(account, Password));

        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal(invalidStatus ? "CK_Users_Status" : "CK_Users_SecurityVersion", postgres.ConstraintName);
    }

    private static IdentityAccount NewAccount()
        => new($"identity-{Guid.NewGuid():N}@example.test", DateTimeOffset.UtcNow);

    private static void AssertSuccess(IdentityResult result)
        => Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Code)));
}