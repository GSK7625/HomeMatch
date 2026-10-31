namespace HomeMatch.Modules.Identity.Domain.Entities;

/// <summary>Canonical credential row from identity.user_tokens. Raw tokens are never stored.</summary>
public class UserToken
{
    protected UserToken() { }
    public UserToken(long userId, string tokenHash, string tokenType, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        if (tokenType is not ("REFRESH" or "RESET_PASSWORD" or "REMEMBER_ME")) throw new ArgumentException("Invalid token type.", nameof(tokenType));
        if (tokenHash.Length != 64 || !tokenHash.All(char.IsAsciiHexDigit)) throw new ArgumentException("Invalid token hash.", nameof(tokenHash));
        if (expiresAt <= createdAt) throw new ArgumentException("Invalid expiry.", nameof(expiresAt));
        UserId = userId; TokenHash = tokenHash; TokenType = tokenType;
        CreatedAt = createdAt.ToUniversalTime(); ExpiresAt = expiresAt.ToUniversalTime();
    }
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string TokenType { get; private set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
