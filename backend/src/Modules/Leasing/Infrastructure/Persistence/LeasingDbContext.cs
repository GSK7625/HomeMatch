using HomeMatch.Modules.Leasing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Leasing.Infrastructure.Persistence;

public class LeasingDbContext(DbContextOptions<LeasingDbContext> options) : DbContext(options)
{
    public const string Schema = "leasing";

    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<DepositTransaction> DepositTransactions => Set<DepositTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeasingDbContext).Assembly);
    }
}
