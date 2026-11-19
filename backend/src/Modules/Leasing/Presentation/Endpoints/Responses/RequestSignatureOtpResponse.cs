namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record RequestSignatureOtpResponse(
    string MaskedEmail,
    DateTimeOffset ExpiresAt,
    string Message);
