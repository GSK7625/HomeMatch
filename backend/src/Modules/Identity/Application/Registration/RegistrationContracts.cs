namespace HomeMatch.Modules.Identity.Application.Registration;

public sealed record RegisterAccountCommand(string? Email, string? Password, string? FullName, string? Role);
public sealed record ValidatedRegistration(string Email, string NormalizedEmail, string Password, string FullName, string Role);

public enum RegistrationOutcome { Accepted, Invalid, Unavailable }

public interface INewPasswordPolicy
{
    bool IsAllowed(string? password);
}

public interface IPasswordBlocklist
{
    bool Contains(string password);
}

public interface IRegistrationStore
{
    Task<RegistrationOutcome> CreateIfAbsentAsync(ValidatedRegistration registration, CancellationToken cancellationToken);
}
