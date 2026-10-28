using HomeMatch.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

internal sealed class IdentityRoleSeeder(IdentityDbContext database)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        foreach (var role in AccountRoles.All)
        {
            // Parameterized SQL with a unique index also handles two initializers racing.
            await database.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO identity."Roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
                VALUES ({Guid.NewGuid()}, {role}, {role.ToUpperInvariant()}, {Guid.NewGuid().ToString("N")})
                ON CONFLICT ("NormalizedName") DO NOTHING
                """, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}