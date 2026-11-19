using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractDetails;

public sealed class GetContractDetailsHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdentityUserReader _userReader;
    private readonly IRoomListingReader _roomReader;

    public GetContractDetailsHandler(
        IContractRepository contractRepository,
        IIdentityUserReader userReader,
        IRoomListingReader roomReader)
    {
        _contractRepository = contractRepository;
        _userReader = userReader;
        _roomReader = roomReader;
    }

    public async Task<Result<ContractDetailDto>> HandleAsync(
        GetContractDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetDetailsByIdAsync(query.Id, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDetailDto>(LeasingErrors.NotFound);
        }

        if (query.RequesterId > 0 &&
            contract.LandlordId != query.RequesterId &&
            contract.TenantId != query.RequesterId)
        {
            return Result.Failure<ContractDetailDto>(LeasingErrors.Forbidden);
        }

        // Bổ sung đầy đủ thông tin thật của Chủ nhà, Người thuê, Phòng, Địa chỉ & Biểu phí
        var enrichedSnapshot = await ContractSnapshotEnricher.BuildEnrichedSnapshotAsync(
            contract.DocumentSnapshot,
            contract.LandlordId,
            contract.TenantId,
            contract.RoomId,
            _userReader,
            _roomReader,
            cancellationToken);

        var dto = ContractMapper.ToDetailDto(contract) with { DocumentSnapshot = enrichedSnapshot };
        return Result.Success(dto);
    }
}
