namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public record LoginRequest(
    string Email,
    string Password
);
