namespace HomeMatch.Modules.Identity.Application.Authentication;

public sealed record AccessTokenSubject(Guid UserId, Guid SessionId, string Role,
    long SecurityVersion, DateTimeOffset SessionExpiresAt);

public sealed record IssuedAccessToken(string AccessToken, string TokenType, int ExpiresIn,
    DateTime ExpiresAt, DateTime SessionExpiresAt);

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenSubject subject);
}

public sealed record AccessTokenIdentity(Guid UserId, Guid SessionId, string Role,
    long SecurityVersion, DateTimeOffset ExpiresAt);

public sealed record CurrentUserProfile(Guid Id, string Email, bool EmailVerified, string Role,
    string FullName, string? PhoneNumber, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AuthenticatedIdentity(Guid UserId, Guid SessionId, string Role, CurrentUserProfile Profile);

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