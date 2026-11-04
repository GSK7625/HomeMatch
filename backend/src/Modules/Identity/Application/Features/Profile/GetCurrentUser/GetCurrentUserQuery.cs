namespace HomeMatch.Modules.Identity.Application.Features.Profile.GetCurrentUser;

public record GetCurrentUserQuery(
    long UserId,
    int? TokenSecurityVersion = null
);

public record CurrentUserResult(
    long Id,
    string Email,
    bool EmailVerified,
    string Role,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
