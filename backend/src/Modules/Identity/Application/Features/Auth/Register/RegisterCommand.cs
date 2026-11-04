namespace HomeMatch.Modules.Identity.Application.Features.Auth.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string Role
);

public record RegisterResult(string Message);
