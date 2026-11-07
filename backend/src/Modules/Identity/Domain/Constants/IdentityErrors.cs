namespace HomeMatch.Modules.Identity.Domain.Constants;

public static class IdentityErrors
{
    public const string ValidationFailed = "Identity.ValidationFailed";
    public const string InvalidCredentials = "Identity.InvalidCredentials";
    public const string EmailNotVerified = "Identity.EmailNotVerified";
    public const string AccountSuspended = "Identity.AccountSuspended";
    public const string TokenInvalid = "Identity.TokenInvalid";
    public const string SessionInvalid = "Identity.SessionInvalid";
    public const string AuthenticationRequired = "Identity.AuthenticationRequired";
    public const string OriginNotAllowed = "Identity.OriginNotAllowed";
    public const string CsrfInvalid = "Identity.CsrfInvalid";
    public const string RateLimitExceeded = "Identity.RateLimitExceeded";
}
