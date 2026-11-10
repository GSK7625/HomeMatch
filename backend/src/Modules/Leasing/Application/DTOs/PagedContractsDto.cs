namespace HomeMatch.Modules.Leasing.Application.DTOs;

public sealed record PagedContractsDto(
    IReadOnlyList<ContractDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
