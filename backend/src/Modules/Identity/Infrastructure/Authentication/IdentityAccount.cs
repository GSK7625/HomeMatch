using HomeMatch.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace HomeMatch.Modules.Identity.Infrastructure.Authentication;

// Framework credential model stays in Infrastructure; endpoints must return explicit DTOs.
public sealed class IdentityAccount : IdentityUser<long>
{
    private IdentityAccount() { }

    public IdentityAccount(string email, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Email = email.Trim();
        UserName = Email;
        LockoutEnabled = false;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public string Role { get; set; } = "Tenant";

    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public long SecurityVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}