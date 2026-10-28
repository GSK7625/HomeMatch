using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Domain.Entities;

public sealed class UserProfile
{
    private UserProfile() { }

    public UserProfile(Guid userId, string fullName, DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A profile must belong to an account.", nameof(userId));
        }

        if (!AccountInput.TryNormalizeName(fullName, out var name))
        {
            throw new ArgumentException("Full name must contain 2–100 valid Unicode characters without controls.", nameof(fullName));
        }

        UserId = userId;
        FullName = name;
        CreatedAt = createdAt.ToUniversalTime();
        UpdatedAt = CreatedAt;
    }

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}