using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Identity;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.CreateContract;

public sealed class CreateContractHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IRoomListingReader _roomReader;
    private readonly IIdentityUserReader _userReader;
    private readonly ILogger<CreateContractHandler> _logger;

    public CreateContractHandler(
        IContractRepository contractRepository,
        IRoomListingReader roomReader,
        IIdentityUserReader userReader,
        ILogger<CreateContractHandler> logger)
    {
        _contractRepository = contractRepository;
        _roomReader = roomReader;
        _userReader = userReader;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        CreateContractCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.LandlordId <= 0 || command.TenantId <= 0 || command.RoomId <= 0)
        {
            return Result.Failure<ContractDto>(new Error("Leasing.InvalidIdentifiers", "ID Chủ nhà, Người thuê hoặc Phòng không hợp lệ."));
        }

        if (command.TenantId == command.LandlordId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.SelfLeasingNotAllowed);
        }

        if (command.EndDate <= command.StartDate)
        {
            return Result.Failure<ContractDto>(LeasingErrors.InvalidDateRange);
        }

        if (command.MonthlyRent <= 0)
        {
            return Result.Failure<ContractDto>(LeasingErrors.InvalidMonthlyRent);
        }

        if (command.DepositAmount < 0)
        {
            return Result.Failure<ContractDto>(LeasingErrors.InvalidDepositAmount);
        }

        // 1. Kiểm tra phòng tồn tại và thuộc về chủ nhà
        var room = await _roomReader.GetRoomAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.RoomNotFound);
        }

        if (room.LandlordId != command.LandlordId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.RoomNotOwnedByLandlord);
        }

        // 2. Kiểm tra tài khoản người thuê tồn tại
        var tenant = await _userReader.GetUserByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.TenantNotFound);
        }

        // 3. Kiểm tra trùng lặp lịch phòng (BR-14)
        var hasOverlap = await _contractRepository.HasOverlappingOccupancyAsync(
            command.RoomId,
            command.StartDate,
            command.EndDate,
            excludeContractId: null,
            cancellationToken);

        if (hasOverlap)
        {
            return Result.Failure<ContractDto>(LeasingErrors.ScheduleConflict);
        }

        // 4. Sinh mã hợp đồng nếu chưa truyền
        var code = string.IsNullOrWhiteSpace(command.ContractCode)
            ? $"HD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}"
            : command.ContractCode.Trim();

        var existingWithCode = await _contractRepository.GetByCodeAsync(code, cancellationToken);
        if (existingWithCode is not null)
        {
            return Result.Failure<ContractDto>(new Error("Leasing.DuplicateContractCode", $"Mã hợp đồng '{code}' đã tồn tại trong hệ thống."));
        }

        // 5. Khởi tạo Domain Entity
        var contractResult = Contract.Create(
            code,
            command.RoomId,
            command.TenantId,
            command.LandlordId,
            command.StartDate,
            command.EndDate,
            command.MonthlyRent,
            command.DepositAmount,
            command.BillingDueDay,
            command.ContractFileUrl);

        if (contractResult.IsFailure)
        {
            return Result.Failure<ContractDto>(contractResult.Error);
        }

        var contract = contractResult.Value;
        await _contractRepository.AddAsync(contract, cancellationToken);
        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Đã tạo hợp đồng mới thành công [ID: {ContractId}, Code: {Code}, Room: {RoomId}]",
            contract.Id, contract.ContractCode, contract.RoomId);

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
