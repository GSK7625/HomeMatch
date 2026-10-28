using System.Text.Json;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.EmailVerification;

public sealed record EmailVerificationRequest;
public sealed record VerifyEmailRequest(Guid ChallengeId, string Code);

public sealed class EmailVerificationRequestReader
{
    public Task<IdentityJsonResult<EmailVerificationRequest>> ReadSendAsync(HttpRequest request, CancellationToken cancellationToken)
        => IdentityJsonReader.ReadAsync<EmailVerificationRequest>(request,
            root => root.EnumerateObject().Any() ? null : new(), cancellationToken, new());

    public Task<IdentityJsonResult<VerifyEmailRequest>> ReadVerifyAsync(HttpRequest request, CancellationToken cancellationToken)
        => IdentityJsonReader.ReadAsync<VerifyEmailRequest>(request, root =>
        {
            if (root.EnumerateObject().Count() != 2
                || !root.TryGetProperty("challengeId", out var id) || id.ValueKind != JsonValueKind.String
                || !Guid.TryParseExact(id.GetString(), "D", out var challengeId) || challengeId == Guid.Empty
                || !root.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String
                || code.GetString() is not { Length: 6 } value || !value.All(char.IsAsciiDigit)) return null;
            return new(challengeId, value);
        }, cancellationToken);
}
