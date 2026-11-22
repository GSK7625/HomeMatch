namespace HomeMatch.Modules.Identity.Application.Features.Profile.UpdateProfile;

public record UpdateProfileCommand(
    long UserId,
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    int? TokenSecurityVersion = null
);
