namespace HomeMatch.Modules.Leasing.Application.Queries.GetHandoverDetails;

public sealed record GetHandoverDetailsQuery(
    long ContractId,
    long RequesterId);
