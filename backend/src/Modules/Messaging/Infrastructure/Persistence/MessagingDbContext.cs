using HomeMatch.Modules.Messaging.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Messaging.Infrastructure.Persistence;

public class MessagingDbContext : DbContext
{
    public const string Schema = "messaging";

    public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageDraft> MessageDrafts => Set<MessageDraft>();
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagingDbContext).Assembly);
    }
}
