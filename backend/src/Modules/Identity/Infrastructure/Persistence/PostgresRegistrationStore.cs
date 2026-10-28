using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

public sealed class PostgresRegistrationStore(IdentityDbContext database, UserManager<IdentityAccount> users,
    TimeProvider clock, ILogger<PostgresRegistrationStore> logger) : IRegistrationStore
{
    public async Task<RegistrationOutcome> CreateIfAbsentAsync(ValidatedRegistration registration, CancellationToken cancellationToken)
    {
        if (!AccountRoles.CanSelfRegister(registration.Role))
            throw new ArgumentException("Public registration cannot provision this role.", nameof(registration));

        try
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            if (users.NormalizeEmail(registration.Email) != registration.NormalizedEmail)
                throw new InvalidOperationException("Email normalization is inconsistent.");
            if (!await database.Roles.AnyAsync(role => role.NormalizedName == registration.Role.ToUpperInvariant(), cancellationToken))
                return RegistrationOutcome.Unavailable;
            var account = new IdentityAccount(registration.Email, clock.GetUtcNow());
            // CreateAsync(user, password) validates/hashes before duplicate user lookup.
            // Keep both new and existing email branches on this path; no existence fast-path.
            cancellationToken.ThrowIfCancellationRequested();
            var created = await users.CreateAsync(account, registration.Password);
            cancellationToken.ThrowIfCancellationRequested();
            if (!created.Succeeded)
            {
                if (created.Errors.Any() && created.Errors.All(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
                    return RegistrationOutcome.Accepted; // transaction disposed/rolled back without modifying the old account
                throw new InvalidOperationException("Identity account provisioning failed.");
            }


            var assigned = await users.AddToRoleAsync(account, registration.Role);
            if (!assigned.Succeeded) throw new InvalidOperationException("Identity role provisioning failed.");
            database.Profiles.Add(new UserProfile(account.Id, registration.FullName, account.CreatedAt));
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RegistrationOutcome.Accepted;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation, SchemaName: "identity", TableName: "Users" } postgres
               && postgres.ConstraintName is "UX_Users_NormalizedEmail" or "UserNameIndex")
        {
            // The unique index wins a race even if both UserManager lookups saw no account.
            database.ChangeTracker.Clear();
            return RegistrationOutcome.Accepted;
        }
        catch (Exception exception) when (IdentityDatabaseFailure.IsUnavailable(exception))
        {
            database.ChangeTracker.Clear();
            logger.LogWarning("Identity registration database is unavailable.");
            return RegistrationOutcome.Unavailable;
        }
    }
}
