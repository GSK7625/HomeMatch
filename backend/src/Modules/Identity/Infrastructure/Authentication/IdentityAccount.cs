using HomeMatch.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

// Framework credential model stays in Infrastructure; endpoints must return explicit DTOs.
public sealed class IdentityAccount : IdentityUser<Guid>
{
    private IdentityAccount() { }

    public IdentityAccount(string email, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Id = Guid.NewGuid();
        Email = email.Trim();
        UserName = Email;
        LockoutEnabled = true;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public long SecurityVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}