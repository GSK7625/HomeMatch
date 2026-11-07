namespace HomeMatch.Modules.Identity.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(
    string RawRefreshToken,
    string? DeviceInfo = null,
    string? IpAddress = null
);

public record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTimeOffset ExpiresAt,
    DateTimeOffset SessionExpiresAt
);
