namespace HomeMatch.Modules.Identity.Application.Features.Auth.Logout;

public record LogoutCommand(
    string? RawRefreshToken
);
