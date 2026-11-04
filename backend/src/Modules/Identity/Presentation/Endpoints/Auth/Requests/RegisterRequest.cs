namespace HomeMatch.Modules.Identity.Presentation.Endpoints.Auth.Requests;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role
);
