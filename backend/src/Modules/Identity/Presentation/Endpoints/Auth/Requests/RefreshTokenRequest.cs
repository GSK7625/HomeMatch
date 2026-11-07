namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public record RefreshTokenRequest(
    string? RefreshToken = null
);
