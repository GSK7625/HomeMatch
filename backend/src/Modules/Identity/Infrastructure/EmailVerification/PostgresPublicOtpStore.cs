using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HomeMatch.Modules.Identity.Application.PublicOtp;
using HomeMatch.Shared.Common;
using HomeMatch.Modules.Identity.Domain.Entities;
using HomeMatch.Modules.Identity.Domain.Enums;
using HomeMatch.Modules.Identity.Infrastructure.Authentication;
using HomeMatch.Modules.Identity.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Identity.Infrastructure.EmailVerification;

public sealed class PostgresPublicOtpStore(IdentityDbContext db, UserManager<IdentityAccount> users,
    IPasswordHasher<IdentityAccount> hasher, LoginDummyCredential dummy, ITransactionalEmailSender sender,
    IOptions<EmailVerificationOptions> options, TimeProvider clock) : IPublicOtpStore
{
    public Task<Result<PublicOtpResponse>> SendAsync(string email, string purpose, string? password, CancellationToken ct)
        => ExecuteAsync(() => SendCoreAsync(email, purpose, password, ct));

    private async Task<Result<PublicOtpResponse>> SendCoreAsync(string email, string purpose, string? password, CancellationToken ct)
    {
        if (!options.Value.Enabled || !sender.IsAvailable) return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Unavailable);
        var id = Guid.NewGuid(); var now = PostgresTimestampHelper.UtcNow(clock);
        var code = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        Result<PublicOtpResponse> accepted = Result.Success(new PublicOtpResponse("Submitted", id, now.AddMinutes(5), RetryAfterSeconds: 60));
        IdentityAccount? account;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            // Global sender quota is locked before account rows, matching the existing email flow.
            var senderWindow = await WindowAsync("sender:" + sender.SenderKey, now, TimeSpan.FromDays(1), true, ct);
            account = (await db.Users.FromSqlInterpolated($"SELECT * FROM identity.\"users\" WHERE \"normalized_email\" = {email} FOR UPDATE").ToListAsync(ct)).SingleOrDefault();
            if (purpose == "Registration")
            {
                if (account is null) dummy.Verify(hasher, password ?? "");
                if (account is null || !await users.CheckPasswordAsync(account, password ?? "")
                    || account.EmailConfirmed || account.Status != AccountStatus.Active) return accepted;
            }
            else if (account is null || !account.EmailConfirmed || account.Status != AccountStatus.Active) return accepted;
            var latest = await db.PublicOtpChallenges.Where(x => x.UserId == account.Id && x.Purpose == purpose)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            var next = latest?.CreatedAt.AddSeconds(latest.Submitted ? 60 : 120);
            if (next > now) return Result.Failure<PublicOtpResponse>(new PublicOtpRateLimitError((int)Math.Ceiling((next.Value - now).TotalSeconds)));
            var shortWindow = await WindowAsync("public15:" + account.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), now, TimeSpan.FromMinutes(15), false, ct);
            var daily = await WindowAsync("public24:" + account.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), now, TimeSpan.FromDays(1), false, ct);
            var retry = new[] { senderWindow.RetryAfter(now, 100), shortWindow.RetryAfter(now, 5), daily.RetryAfter(now, 20) }
                .Where(x => x.HasValue).Select(x => x!.Value.TotalSeconds).DefaultIfEmpty(0).Max();
            if (retry > 0) return Result.Failure<PublicOtpResponse>(new PublicOtpRateLimitError((int)Math.Ceiling(retry)));
            senderWindow.Consume(now, 100); shortWindow.Consume(now, 5); daily.Consume(now, 20);
            await db.PublicOtpChallenges.Where(x => x.UserId == account.Id && x.Purpose == purpose)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Closed, true).SetProperty(x => x.ResetUsed, true), ct);
            var challenge = new PublicOtpChallenge
            {
                Id = id,
                UserId = account.Id,
                Purpose = purpose,
                Email = account.NormalizedEmail!,
                SecurityVersion = account.SecurityVersion,
                KeyId = options.Value.KeyId,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(5)
            };
            challenge.CodeMac = Mac(challenge, code);
            db.PublicOtpChallenges.Add(challenge);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        // No DB lock is held across SMTP. Pending reservations cannot be verified.
        var outcome = await sender.SendAsync(new TransactionalEmail(account.Email!,
            purpose == "Registration" ? "HomeMatch — Xác thực đăng ký" : "HomeMatch — Khôi phục mật khẩu",
            $"Mã OTP của bạn: {code}\nMã hết hạn sau 5 phút kể từ yêu cầu. Không chia sẻ mã này."), ct);
        db.ChangeTracker.Clear();
        if (outcome == EmailSubmissionOutcome.Accepted)
            await db.PublicOtpChallenges.Where(x => x.Id == id && !x.Closed)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Submitted, true), ct);
        else
            await db.PublicOtpChallenges.Where(x => x.Id == id)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.Closed, true), ct);
        // A generic response avoids exposing mailbox/account existence or SMTP outcomes.
        return accepted;
    }

    public Task<Result<PublicOtpResponse>> VerifyAsync(Guid challengeId, string code, string purpose, CancellationToken ct)
        => ExecuteAsync(() => VerifyCoreAsync(challengeId, code, purpose, ct));

    private async Task<Result<PublicOtpResponse>> VerifyCoreAsync(Guid challengeId, string code, string purpose, CancellationToken ct)
    {
        if (!options.Value.Enabled) return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Unavailable);
        var location = await db.PublicOtpChallenges.AsNoTracking().Where(x => x.Id == challengeId && x.Purpose == purpose)
            .Select(x => (long?)x.UserId).SingleOrDefaultAsync(ct);
        if (location is null) return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Invalid);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var account = await LockAccountAsync(location.Value, ct);
        var challenge = await db.PublicOtpChallenges.SingleAsync(x => x.Id == challengeId, ct);
        var now = PostgresTimestampHelper.UtcNow(clock);
        var quota = await WindowAsync("publicverify:" + location.Value.ToString("N"), now, TimeSpan.FromMinutes(15), false, ct);
        if (quota.RetryAfter(now, 20) is { } retry) return Result.Failure<PublicOtpResponse>(new PublicOtpRateLimitError((int)Math.Ceiling(retry.TotalSeconds)));
        quota.Consume(now, 20);
        Result<PublicOtpResponse> result = Result.Failure<PublicOtpResponse>(PublicOtpErrors.Invalid);
        if (ValidAccount(account, challenge) && challenge.CanVerifyAt(now))
        {
            var expected = Mac(challenge, code);
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(challenge.CodeMac)))
            {
                challenge.RejectCode(now);
            }
            else
            {
                challenge.Closed = true;
                if (purpose == "Registration" && !account!.EmailConfirmed)
                {
                    account.EmailConfirmed = true; account.UpdatedAt = now;
                    account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
                    result = Result.Success(new PublicOtpResponse("Verified"));
                }
                else if (purpose == "PasswordReset" && account!.EmailConfirmed)
                {
                    var secret = RefreshSecrets.Create();
                    challenge.ResetTokenHash = secret.Hash; challenge.ResetExpiresAt = now.AddMinutes(5);
                    db.CredentialTokens.Add(new UserToken(account.Id, secret.Hash, "RESET_PASSWORD", now, challenge.ResetExpiresAt.Value));
                    result = Result.Success(new PublicOtpResponse("Verified", ExpiresAt: challenge.ResetExpiresAt, ResetToken: secret.Raw));
                }
            }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return result;
    }

    public Task<Result<PublicOtpResponse>> ResetAsync(string token, string password, CancellationToken ct)
        => ExecuteAsync(() => ResetCoreAsync(token, password, ct));

    private async Task<Result<PublicOtpResponse>> ResetCoreAsync(string token, string password, CancellationToken ct)
    {
        if (!options.Value.Enabled) return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Unavailable);
        var hash = RefreshSecrets.TryHash(token);
        var location = hash is null ? null : await db.PublicOtpChallenges.AsNoTracking()
            .Where(x => x.ResetTokenHash == hash && x.Purpose == "PasswordReset").Select(x => (long?)x.UserId).SingleOrDefaultAsync(ct);
        if (location is null) return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Invalid);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var account = await LockAccountAsync(location.Value, ct);
        var challenge = await db.PublicOtpChallenges.SingleAsync(x => x.ResetTokenHash == hash, ct);
        var now = PostgresTimestampHelper.UtcNow(clock);
        if (!ValidAccount(account, challenge) || !account!.EmailConfirmed || challenge.ResetUsed || challenge.ResetExpiresAt is null || challenge.ResetExpiresAt <= now)
            return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Invalid);
        if (!await db.CredentialTokens.AnyAsync(token => token.UserId == account.Id && token.TokenHash == hash
            && token.TokenType == "RESET_PASSWORD" && token.ExpiresAt > now, ct))
            return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Invalid);
        account.PasswordHash = hasher.HashPassword(account, password);
        account.SecurityStamp = Guid.NewGuid().ToString("N"); account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        account.SecurityVersion = checked(account.SecurityVersion + 1); account.UpdatedAt = now;
        await SessionRevocationHelper.RevokeAccountAsync(db, account.Id, now, ct);
        await db.PublicOtpChallenges.Where(x => x.UserId == account.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.Closed, true).SetProperty(x => x.ResetUsed, true), ct);
        challenge.ResetUsed = true;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Result.Success(new PublicOtpResponse("Reset"));
    }

    private static async Task<Result<PublicOtpResponse>> ExecuteAsync(Func<Task<Result<PublicOtpResponse>>> operation)
    {
        try { return await operation(); }
        catch (Exception exception) when (DatabaseFailureHelper.IsUnavailable(exception))
        { return Result.Failure<PublicOtpResponse>(PublicOtpErrors.Unavailable); }
    }

    private static bool ValidAccount(IdentityAccount? a, PublicOtpChallenge c) => a is not null
        && a.Status == AccountStatus.Active && a.NormalizedEmail == c.Email && a.SecurityVersion == c.SecurityVersion;
    private async Task<IdentityAccount?> LockAccountAsync(long id, CancellationToken ct) =>
        (await db.Users.FromSqlInterpolated($"SELECT * FROM identity.\"users\" WHERE \"id\" = {id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault();
    private string Mac(PublicOtpChallenge c, string code)
    {
        if (!options.Value.Keys.TryGetValue(c.KeyId, out var key)) return new string('0', 64);
        return Convert.ToHexString(HMACSHA256.HashData(Convert.FromBase64String(key), Encoding.UTF8.GetBytes(
            $"public-otp|{c.Purpose}|{c.Id:D}|{c.UserId:D}|{c.SecurityVersion}|{c.Email}|{code}")));
    }
    private async Task<EmailVerificationQuota> WindowAsync(string key, DateTimeOffset now, TimeSpan length, bool utcDay, CancellationToken ct)
    {
        var start = utcDay ? new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero) : now;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO identity.\"email_verification_quotas\" (\"key\", \"window_start\", \"window_end\", \"count\") VALUES ({key}, {start}, {start + length}, 0) ON CONFLICT (\"key\") DO NOTHING", ct);
        var row = (await db.EmailVerificationQuotas.FromSqlInterpolated($"SELECT * FROM identity.\"email_verification_quotas\" WHERE \"key\" = {key} FOR UPDATE").ToListAsync(ct)).Single();
        row.RenewIfExpired(now, length, utcDay); return row;
    }
}
