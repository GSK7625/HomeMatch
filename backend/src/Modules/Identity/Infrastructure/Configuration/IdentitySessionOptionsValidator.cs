using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

public sealed class IdentitySessionOptionsValidator : IValidateOptions<IdentitySessionOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentitySessionOptions options)
        => options.NormalLifetimeHours is >= 1 and <= 24
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Identity:Sessions:NormalLifetimeHours must be between 1 and 24.");
}
