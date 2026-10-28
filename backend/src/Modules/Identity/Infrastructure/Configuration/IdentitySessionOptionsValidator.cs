using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.Configuration;

public sealed class IdentitySessionOptionsValidator : IValidateOptions<IdentitySessionOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentitySessionOptions options)
    {
        var failures = new List<string>();

        if (options.NormalLifetimeHours is < 1 or > 24)
        {
            failures.Add("Identity:Sessions:NormalLifetimeHours must be between 1 and 24.");
        }

        if (options.RememberLifetimeDays is < 1 or > 30)
        {
            failures.Add("Identity:Sessions:RememberLifetimeDays must be between 1 and 30.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
