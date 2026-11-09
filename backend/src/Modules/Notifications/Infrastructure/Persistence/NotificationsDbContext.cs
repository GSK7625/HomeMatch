using HomeMatch.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Notifications.Infrastructure.Persistence;

public class NotificationsDbContext : DbContext
{
    public const string Schema = "notifications";

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
