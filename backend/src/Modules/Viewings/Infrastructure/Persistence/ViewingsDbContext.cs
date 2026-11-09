using HomeMatch.Modules.Viewings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Viewings.Infrastructure.Persistence;

public class ViewingsDbContext : DbContext
{
    public const string Schema = "viewings";

    public ViewingsDbContext(DbContextOptions<ViewingsDbContext> options) : base(options)
    {
    }

    public DbSet<ViewingAppointment> ViewingAppointments => Set<ViewingAppointment>();
    public DbSet<VirtualViewing> VirtualViewings => Set<VirtualViewing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ViewingsDbContext).Assembly);
    }
}
