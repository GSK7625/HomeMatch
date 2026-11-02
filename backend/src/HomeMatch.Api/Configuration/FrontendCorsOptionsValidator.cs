using Microsoft.Extensions.Options;

namespace HomeMatch.Api.Configuration;

public sealed class FrontendCorsOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<FrontendCorsOptions>
{
    public ValidateOptionsResult Validate(string? name, FrontendCorsOptions options)
    {
        if (options.AllowedOrigins is not { Length: > 0 } || options.AllowedOrigins.Any(origin => !IsValidOrigin(origin)))
        {
            return ValidateOptionsResult.Fail(
                "Cors:AllowedOrigins must contain explicit HTTP(S) origins without paths, trailing slashes, wildcards or credentials; non-Development environments require HTTPS.");
        }

        return ValidateOptionsResult.Success;
    }

    private bool IsValidOrigin(string? origin)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || (environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp))
            && string.IsNullOrEmpty(uri.UserInfo)
            && !uri.Host.Contains('*')
            && string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
    }
}
