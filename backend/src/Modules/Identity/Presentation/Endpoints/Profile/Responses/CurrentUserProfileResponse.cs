namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Profile.Responses;

public record CurrentUserProfileResponse(
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
