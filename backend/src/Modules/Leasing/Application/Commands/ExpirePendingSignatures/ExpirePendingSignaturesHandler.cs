using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.ExpirePendingSignatures;

public sealed class ExpirePendingSignaturesHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdentityUserReader _identityUserReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILeasingEmailService _leasingEmailService;
    private readonly ILogger<ExpirePendingSignaturesHandler> _logger;

    public ExpirePendingSignaturesHandler(
        IContractRepository contractRepository,
        IIdentityUserReader identityUserReader,
        INotificationPublisher notificationPublisher,
        ILeasingEmailService leasingEmailService,
        ILogger<ExpirePendingSignaturesHandler> logger)
    {
        _contractRepository = contractRepository;
        _identityUserReader = identityUserReader;
        _notificationPublisher = notificationPublisher;
        _leasingEmailService = leasingEmailService;
        _logger = logger;
    }

    public async Task<Result<ExpirePendingSignaturesResult>> HandleAsync(
        ExpirePendingSignaturesCommand command,
        CancellationToken cancellationToken = default)
    {
        var asOf = command.AsOf ?? DateTimeOffset.UtcNow;
        var expiredContracts = await _contractRepository.GetPendingSignatureExpiredContractsAsync(asOf, cancellationToken);

        var totalProcessed = expiredContracts.Count;
        var expiredCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        _logger.LogInformation(
            "Bắt đầu quét chữ ký hết hạn tại {AsOf}. Tìm thấy {Count} hợp đồng.",
            asOf, totalProcessed);

        foreach (var contract in expiredContracts)
        {
            try
            {
                var expireResult = contract.ExpireSignature(asOf);
                if (expireResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Không thể chuyển trạng thái hết hạn ký cho hợp đồng #{ContractCode} [ID: {ContractId}]: {Error}",
                        contract.ContractCode, contract.Id, expireResult.Error.Message);
                    skippedCount++;
                    continue;
                }

                await _contractRepository.SaveChangesAsync(cancellationToken);
                expiredCount++;

                _logger.LogInformation(
                    "Hợp đồng #{ContractCode} [ID: {ContractId}] đã chuyển sang SIGNATURE_EXPIRED. Giải phóng giữ phòng.",
                    contract.ContractCode, contract.Id);

                // Gửi thông báo trong ứng dụng qua Notifications Module
                await PublishNotificationsSafelyAsync(contract, cancellationToken);

                // Gửi email thông báo cho các bên
                await SendEmailsSafelyAsync(contract, cancellationToken);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex,
                    "Lỗi khi xử lý hết hạn chữ ký cho hợp đồng #{ContractCode} [ID: {ContractId}]: {Message}",
                    contract.ContractCode, contract.Id, ex.Message);
            }
        }

        _logger.LogInformation(
            "Hoàn tất quét chữ ký hết hạn. Tổng: {Total}, Thành công: {Expired}, Bỏ qua: {Skipped}, Lỗi: {Errors}",
            totalProcessed, expiredCount, skippedCount, errorCount);

        return Result.Success(new ExpirePendingSignaturesResult(
            totalProcessed, expiredCount, skippedCount, errorCount));
    }

    private async Task PublishNotificationsSafelyAsync(
        Domain.Entities.Contract contract,
        CancellationToken cancellationToken)
    {
        try
        {
            // Báo cho Chủ nhà
            await _notificationPublisher.PublishAsync(
                userId: contract.LandlordId,
                title: "Hợp đồng thuê đã quá hạn ký",
                content: $"Hợp đồng thuê #{contract.ContractCode} đã quá thời hạn ký và tự động hết hiệu lực. Lịch giữ phòng đã được giải phóng.",
                type: NotificationTypeConstants.ContractSignatureExpired,
                referenceType: "CONTRACT",
                referenceId: contract.Id,
                cancellationToken: cancellationToken);

            // Báo cho Người thuê
            await _notificationPublisher.PublishAsync(
                userId: contract.TenantId,
                title: "Hợp đồng thuê đã quá hạn ký",
                content: $"Hợp đồng thuê #{contract.ContractCode} đã hết thời hạn ký trực tuyến và không còn hiệu lực.",
                type: NotificationTypeConstants.ContractSignatureExpired,
                referenceType: "CONTRACT",
                referenceId: contract.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Lỗi khi phát notification cho hợp đồng hết hạn ký #{ContractCode}",
                contract.ContractCode);
        }
    }

    private async Task SendEmailsSafelyAsync(
        Domain.Entities.Contract contract,
        CancellationToken cancellationToken)
    {
        try
        {
            var landlord = await _identityUserReader.GetUserByIdAsync(contract.LandlordId, cancellationToken);
            if (landlord is not null && !string.IsNullOrWhiteSpace(landlord.Email))
            {
                await _leasingEmailService.SendSignatureExpiredEmailAsync(
                    landlord.Email, landlord.FullName, contract.ContractCode, cancellationToken);
            }

            var tenant = await _identityUserReader.GetUserByIdAsync(contract.TenantId, cancellationToken);
            if (tenant is not null && !string.IsNullOrWhiteSpace(tenant.Email))
            {
                await _leasingEmailService.SendSignatureExpiredEmailAsync(
                    tenant.Email, tenant.FullName, contract.ContractCode, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Lỗi khi gửi email thông báo hết hạn ký cho hợp đồng #{ContractCode}",
                contract.ContractCode);
        }
    }
}
