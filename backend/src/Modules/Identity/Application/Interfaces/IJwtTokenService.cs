namespace HomeMatch.Modules.Identity.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(long userId, string email, string role, string fullName, int securityVersion);
    int AccessTokenExpirationSeconds { get; }
}
