namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public sealed record PagedContractsResponse(
    IReadOnlyList<ContractResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
