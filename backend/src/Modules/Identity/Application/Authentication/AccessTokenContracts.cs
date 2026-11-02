namespace HomeMatch.Modules.Identity.Application.Authentication;

public sealed record AccessTokenSubject(long UserId, Guid SessionId, string Role,
    long SecurityVersion, DateTimeOffset SessionExpiresAt);

public sealed record IssuedAccessToken(string AccessToken, string TokenType, int ExpiresIn,
    DateTime ExpiresAt, DateTime SessionExpiresAt);

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenSubject subject);
}

public sealed record AccessTokenIdentity(long UserId, Guid SessionId, string Role,
    long SecurityVersion, DateTimeOffset ExpiresAt);

public sealed record CurrentUserProfile(long Id, string Email, bool EmailVerified, string Role,
    string FullName, string? PhoneNumber, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AuthenticatedIdentity(long UserId, Guid SessionId, string Role, CurrentUserProfile Profile);

public interface IAccessIdentityReader
{
    Task<AuthenticatedIdentity?> FindActiveAsync(AccessTokenIdentity identity, CancellationToken cancellationToken);
}

public interface ICurrentIdentity
{
    AuthenticatedIdentity? Identity { get; }
}

/// <summary>No positive whole-second JWT lifetime remains in the session.</summary>
public sealed class AccessTokenSessionExpiredException()
    : ArgumentException("The session has expired.", "subject");