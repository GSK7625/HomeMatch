namespace HomeMatch.Modules.Leasing.Application.Commands.RequestSignatureOtp;

public sealed record RequestSignatureOtpCommand(
    long ContractId,
    long ActorId);

public sealed record RequestSignatureOtpResult(
    string MaskedEmail,
    DateTimeOffset ExpiresAt,
    string Message);
