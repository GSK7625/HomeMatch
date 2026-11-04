namespace HomeMatch.Shared.Contracts.Identity;

public sealed record IdentityUserInfo(
    long Id,
    string FullName,
    string Email,
    string? PhoneNumber = null,
    string? Role = null
);
