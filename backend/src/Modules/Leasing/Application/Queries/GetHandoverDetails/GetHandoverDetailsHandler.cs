using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetHandoverDetails;

public sealed class GetHandoverDetailsHandler
{
    private readonly IContractRepository _contractRepository;

    public GetHandoverDetailsHandler(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public async Task<Result<HandoverDetailsDto>> HandleAsync(
        GetHandoverDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(query.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<HandoverDetailsDto>(LeasingErrors.NotFound);
        }

        if (query.RequesterId > 0 &&
            contract.LandlordId != query.RequesterId &&
            contract.TenantId != query.RequesterId)
        {
            return Result.Failure<HandoverDetailsDto>(LeasingErrors.Forbidden);
        }

        var isHandoverConfirmed = contract.HandoverConfirmedAt.HasValue;
        
        // Quyền sử dụng phòng được giải phóng khi đã hoàn tất bàn giao hoặc hợp đồng đã hủy/từ chối/hết hạn ký
        var isOccupancyReleased = contract.Status == ContractStatus.Completed
            || contract.Status == ContractStatus.Cancelled
            || contract.Status == ContractStatus.Rejected
            || contract.Status == ContractStatus.SignatureExpired
            || (contract.Status == ContractStatus.Terminated && isHandoverConfirmed);

        var dto = new HandoverDetailsDto(
            ContractId: contract.Id,
            ContractCode: contract.ContractCode,
            RoomId: contract.RoomId,
            ContractStatus: contract.Status,
            StartDate: contract.StartDate,
            EndDate: contract.EndDate,
            HandoverDueAt: contract.HandoverDueAt,
            IsHandoverConfirmed: isHandoverConfirmed,
            HandoverConfirmedAt: contract.HandoverConfirmedAt,
            HandoverConfirmedBy: contract.HandoverConfirmedBy,
            ActualOccupancyEndDate: contract.ActualOccupancyEndDate,
            IsOccupancyReleased: isOccupancyReleased);

        return Result.Success(dto);
    }
}
