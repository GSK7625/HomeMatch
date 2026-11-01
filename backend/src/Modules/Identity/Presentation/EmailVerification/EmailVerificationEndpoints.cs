using HomeMatch.Modules.Identity.Application.Authentication;
using HomeMatch.Modules.Identity.Application.EmailVerification;
using HomeMatch.Modules.Identity.Presentation.Http;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace HomeMatch.Modules.Identity.Presentation.EmailVerification;

public static class EmailVerificationEndpoints
{
    public static async Task<IResult> RequestAsync(HttpContext context, ICurrentIdentity current,
        RequestEmailVerificationHandler handler, IdentityBody<EmptyIdentityRequest> body)
    {
        if (current.Identity is not { } identity) return AuthenticationRequired(context);
        if (body.Error != IdentityJsonError.None) return BodyFailure(body.Error);
        var result = await handler.HandleAsync(new(identity.UserId, identity.SessionId, identity.Role), context.RequestAborted);
        return Map(context, result);
    }

    public static async Task<IResult> VerifyAsync(HttpContext context, ICurrentIdentity current,
        IEmailVerificationStore store, IdentityBody<VerifyEmailRequest> body)
    {
        if (current.Identity is not { } identity) return AuthenticationRequired(context);
        if (body.Error != IdentityJsonError.None) return BodyFailure(body.Error);
        if (body.Value!.ChallengeId == Guid.Empty || body.Value.Code is not { Length: 6 } code
            || !code.All(char.IsAsciiDigit)) return IdentityHttpErrors.ValidationFailed();
        var result = await store.VerifyAsync(new(identity.UserId, identity.SessionId, identity.Role),
            body.Value.ChallengeId, code, context.RequestAborted);
        return Map(context, result);
    }

    private static IResult Map(HttpContext context, VerificationResult result) => result.Outcome switch
    {
        VerificationOutcome.Submitted => Results.Ok(ApiResponse<SendVerificationResponse>.Success(
            new("Submitted", result.ChallengeId, result.ExpiresAt?.UtcDateTime, (int)(result.RetryAfter?.TotalSeconds ?? 0)))),
        VerificationOutcome.AlreadyVerified => Results.Ok(ApiResponse<SendVerificationResponse>.Success(new("AlreadyVerified", null, null, 0))),
        VerificationOutcome.Verified => Results.Ok(ApiResponse<VerifiedResponse>.Success(new(true))),
        VerificationOutcome.InvalidAuthentication => AuthenticationRequired(context),
        VerificationOutcome.InvalidCode => IdentityHttpErrors.EmailVerificationInvalid(),
        VerificationOutcome.RateLimited => RateLimited(context, result.RetryAfter ?? TimeSpan.FromSeconds(1)),
        _ => IdentityHttpErrors.DependencyUnavailable()
    };

    private static IResult AuthenticationRequired(HttpContext context)
    {
        context.Response.Headers.WWWAuthenticate = "Bearer";
        return IdentityHttpErrors.AuthenticationRequired();
    }
    private static IResult RateLimited(HttpContext context, TimeSpan retry)
    {
        context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        return IdentityHttpErrors.RateLimited();
    }
    private static IResult BodyFailure(IdentityJsonError error) => error switch
    {
        IdentityJsonError.TooLarge => IdentityHttpErrors.RequestTooLarge(),
        IdentityJsonError.UnsupportedMediaType => IdentityHttpErrors.UnsupportedMediaType(),
        _ => IdentityHttpErrors.ValidationFailed()
    };

    public sealed record SendVerificationResponse(string Status, Guid? ChallengeId, DateTime? ExpiresAt, int RetryAfterSeconds);
    public sealed record VerifiedResponse(bool EmailVerified);
}
