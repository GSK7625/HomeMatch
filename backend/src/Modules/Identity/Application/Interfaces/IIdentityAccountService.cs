using HomeMatch.Modules.Identity.Domain.Enums;

namespace HomeMatch.Modules.Identity.Application.Interfaces;

public record CreateUserResult(bool Succeeded, long UserId, IEnumerable<string> Errors);
public record OperationResult(bool Succeeded, IEnumerable<string> Errors);

public record VerifyCredentialsResult(
    bool Succeeded,
    long UserId,
    string Email,
    string Role,
    UserStatus Status,
    bool EmailConfirmed,
    int SecurityVersion,
    string? ErrorCode = null
);

public record UserAccountDetails(
    long UserId,
    string Email,
    string Role,
    UserStatus Status,
    bool EmailConfirmed,
    int SecurityVersion,
    string? PhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public interface IIdentityAccountService
{
    Task<CreateUserResult> CreateUserAsync(string email, string password, string role, CancellationToken cancellationToken = default);
    Task<string> GenerateEmailConfirmationTokenAsync(long userId, CancellationToken cancellationToken = default);
    Task<OperationResult> ConfirmEmailAsync(long userId, string token, CancellationToken cancellationToken = default);
    Task<bool> CheckEmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsEmailConfirmedAsync(string email, CancellationToken cancellationToken = default);
    Task<long?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<VerifyCredentialsResult> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<UserAccountDetails?> GetUserDetailsByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<int> IncrementSecurityVersionAsync(long userId, CancellationToken cancellationToken = default);
    Task<string> GeneratePasswordResetTokenAsync(long userId, CancellationToken cancellationToken = default);
    Task<OperationResult> ResetPasswordAsync(long userId, string token, string newPassword, CancellationToken cancellationToken = default);
}
