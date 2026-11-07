namespace HomeMatch.Modules.Identity.Application.Features.Auth.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? DeviceInfo = null,
    string? IpAddress = null
);

public record LoginResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTimeOffset ExpiresAt,
    DateTimeOffset SessionExpiresAt
);
