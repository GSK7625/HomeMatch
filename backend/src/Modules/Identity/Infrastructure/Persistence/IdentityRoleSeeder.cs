namespace HomeMatch.Modules.Identity.Infrastructure.Persistence;

/// <summary>Roles are constrained values on users.role in the canonical SQL model.</summary>
internal sealed class IdentityRoleSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
