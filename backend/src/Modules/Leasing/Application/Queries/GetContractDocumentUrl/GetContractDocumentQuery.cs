namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractDocumentUrl;

public sealed record GetContractDocumentQuery(
    long ContractId,
    long ActorId);

public sealed record ContractDocumentUrlDto(
    string DownloadUrl,
    string? DocumentHash,
    string ObjectKey,
    DateTimeOffset ExpiresAt);
