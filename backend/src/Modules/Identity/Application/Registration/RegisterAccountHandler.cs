using HomeMatch.Modules.Identity.Domain;
using HomeMatch.Modules.Identity.Domain.Validation;

namespace HomeMatch.Modules.Identity.Application.Registration;

public sealed class RegisterAccountHandler(INewPasswordPolicy passwords, IRegistrationStore store)
{
    public Task<RegistrationOutcome> HandleAsync(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        if (!AccountInput.TryNormalizeEmail(command.Email, out var email, out var normalized)
            || !AccountInput.TryNormalizeName(command.FullName, out var name)
            || !AccountRoles.CanSelfRegister(command.Role) || !passwords.IsAllowed(command.Password))
            return Task.FromResult(RegistrationOutcome.Invalid);

        return store.CreateIfAbsentAsync(new ValidatedRegistration(email, normalized,
            command.Password!, name, command.Role!), cancellationToken);
    }
}
