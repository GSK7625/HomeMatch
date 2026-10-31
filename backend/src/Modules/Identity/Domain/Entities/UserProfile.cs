using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Domain.Entities;

public sealed class UserProfile
{
    private UserProfile() { }

    public UserProfile(long userId, string fullName, DateTimeOffset createdAt)
    {
        if (userId <= 0)
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

    public long UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool NotifyEmail { get; set; } = true;
    public bool NotifySms { get; set; }
    public bool NotifyPush { get; set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}