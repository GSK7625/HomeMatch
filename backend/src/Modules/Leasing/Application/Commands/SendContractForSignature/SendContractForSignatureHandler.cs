using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.SendContractForSignature;

public sealed class SendContractForSignatureHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<SendContractForSignatureHandler> _logger;

    public SendContractForSignatureHandler(
        IContractRepository contractRepository,
        INotificationPublisher notificationPublisher,
        ILogger<SendContractForSignatureHandler> logger)
    {
        _contractRepository = contractRepository;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        SendContractForSignatureCommand command,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        if (contract.LandlordId != command.LandlordId)
        {
            return Result.Failure<ContractDto>(LeasingErrors.Forbidden);
        }

        // Kiểm tra lịch giữ phòng trước khi chuyển sang PENDING_SIGNATURE
        var hasOverlap = await _contractRepository.HasOverlappingOccupancyAsync(
            contract.RoomId,
            contract.StartDate,
            contract.EndDate,
            excludeContractId: contract.Id,
            cancellationToken);

        if (hasOverlap)
        {
            return Result.Failure<ContractDto>(LeasingErrors.ScheduleConflict);
        }

        var result = contract.SendForSignature(command.SignatureExpiresAt, command.HandoverDueAt);
        if (result.IsFailure)
        {
            return Result.Failure<ContractDto>(result.Error);
        }

        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hợp đồng [ID: {ContractId}] đã được gửi ký. Hạn chót: {ExpiresAt}",
            contract.Id, command.SignatureExpiresAt);

        try
        {
            await _notificationPublisher.PublishAsync(
                userId: contract.TenantId,
                title: "Yêu cầu ký hợp đồng thuê mới",
                content: $"Chủ nhà đã gửi hợp đồng thuê #{contract.ContractCode} để bạn xem và ký xác thực. Hạn chót: {command.SignatureExpiresAt:dd/MM/yyyy HH:mm} UTC.",
                type: NotificationTypeConstants.ContractPendingSignature,
                referenceType: "CONTRACT",
                referenceId: contract.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo gửi ký hợp đồng #{ContractCode}", contract.ContractCode);
        }

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
