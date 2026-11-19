namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record ContractDocumentUrlResponse(
    string DownloadUrl,
    string? DocumentHash,
    string ObjectKey,
    DateTimeOffset ExpiresAt);
