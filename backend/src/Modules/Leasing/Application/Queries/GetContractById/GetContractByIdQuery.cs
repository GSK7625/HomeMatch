namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractById;

public sealed record GetContractByIdQuery(
    long Id,
    long RequesterId);
