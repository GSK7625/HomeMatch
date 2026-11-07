namespace HomeMatch.Modules.Identity.Domain.Entities;

public class UserProfile
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; } // MALE, FEMALE, OTHER
    public bool NotifyEmail { get; set; } = true;
    public bool NotifySms { get; set; } = false;
    public bool NotifyPush { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
