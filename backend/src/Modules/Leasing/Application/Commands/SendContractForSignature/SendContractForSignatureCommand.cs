namespace HomeMatch.Modules.Leasing.Application.Commands.SendContractForSignature;

public sealed record SendContractForSignatureCommand(
    long ContractId,
    long LandlordId,
    DateTimeOffset SignatureExpiresAt,
    DateTimeOffset? HandoverDueAt = null);
