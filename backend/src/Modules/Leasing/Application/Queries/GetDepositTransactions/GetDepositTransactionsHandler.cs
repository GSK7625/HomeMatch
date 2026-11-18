using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetDepositTransactions;

public sealed class GetDepositTransactionsHandler
{
    private readonly IContractRepository _contractRepository;

    public GetDepositTransactionsHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<DepositHistoryDto>> HandleAsync(
        GetDepositTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetDetailsByIdAsync(query.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<DepositHistoryDto>(LeasingErrors.NotFound);
        }

        if (query.RequesterId > 0 &&
            contract.LandlordId != query.RequesterId &&
            contract.TenantId != query.RequesterId)
        {
            return Result.Failure<DepositHistoryDto>(LeasingErrors.Forbidden);
        }

        var dto = new DepositHistoryDto(
            contract.Id,
            contract.DepositAmount,
            contract.GetTotalReceived(),
            contract.GetTotalRefunded(),
            contract.GetTotalDeducted(),
            contract.GetCurrentDepositBalance(),
            contract.DepositTransactions.Select(ContractMapper.ToDto).ToList());

        return Result.Success(dto);
    }
}
