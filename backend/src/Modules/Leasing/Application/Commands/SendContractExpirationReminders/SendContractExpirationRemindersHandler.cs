using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.SendContractExpirationReminders;

public sealed class SendContractExpirationRemindersHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdentityUserReader _identityUserReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILeasingEmailService _leasingEmailService;
    private readonly ILogger<SendContractExpirationRemindersHandler> _logger;

    public SendContractExpirationRemindersHandler(
        IContractRepository contractRepository,
        IIdentityUserReader identityUserReader,
        INotificationPublisher notificationPublisher,
        ILeasingEmailService leasingEmailService,
        ILogger<SendContractExpirationRemindersHandler> logger)
    {
        _contractRepository = contractRepository;
        _identityUserReader = identityUserReader;
        _notificationPublisher = notificationPublisher;
        _leasingEmailService = leasingEmailService;
        _logger = logger;
    }

    public async Task<Result<SendContractExpirationRemindersResult>> HandleAsync(
        SendContractExpirationRemindersCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = command.Now ?? DateTimeOffset.UtcNow;
        var today = command.Today ?? DateOnly.FromDateTime(now.Date);
        var daysThreshold = command.DaysThreshold > 0 ? command.DaysThreshold : 30;

        var contracts = await _contractRepository.GetContractsNeedingExpirationReminderAsync(
            today, daysThreshold, cancellationToken);

        var totalCandidates = contracts.Count;
        var sentCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        _logger.LogInformation(
            "Bắt đầu quét gửi nhắc nhở hết hạn hợp đồng (ngưỡng: {Threshold} ngày). Tìm thấy {Count} hợp đồng phù hợp.",
            daysThreshold, totalCandidates);

        foreach (var contract in contracts)
        {
            try
            {
                var daysRemaining = contract.EndDate.DayNumber - today.DayNumber;
                if (daysRemaining < 0)
                {
                    skippedCount++;
                    continue;
                }

                // Gửi Notification trong app cho Chủ nhà
                await _notificationPublisher.PublishAsync(
                    userId: contract.LandlordId,
                    title: "Hợp đồng thuê sắp hết hạn",
                    content: $"Hợp đồng #{contract.ContractCode} sẽ hết hạn vào ngày {contract.EndDate:dd/MM/yyyy} (còn {daysRemaining} ngày). Vui lòng lên kế hoạch gia hạn hoặc chuẩn bị bàn giao phòng.",
                    type: NotificationTypeConstants.ContractExpiring,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                // Gửi Email cho Chủ nhà
                var landlord = await _identityUserReader.GetUserByIdAsync(contract.LandlordId, cancellationToken);
                if (landlord is not null && !string.IsNullOrWhiteSpace(landlord.Email))
                {
                    await _leasingEmailService.SendContractExpiringReminderEmailAsync(
                        landlord.Email, landlord.FullName, contract.ContractCode, contract.EndDate, daysRemaining, cancellationToken);
                }

                // Nếu còn dưới 7 ngày -> nhắc nhở cả Người thuê
                if (daysRemaining <= 7)
                {
                    await _notificationPublisher.PublishAsync(
                        userId: contract.TenantId,
                        title: "Hợp đồng thuê sắp hết hạn",
                        content: $"Hợp đồng #{contract.ContractCode} sẽ hết hạn vào ngày {contract.EndDate:dd/MM/yyyy} (còn {daysRemaining} ngày). Vui lòng kiểm tra và chuẩn bị bàn giao lại phòng.",
                        type: NotificationTypeConstants.ContractExpiring,
                        referenceType: "CONTRACT",
                        referenceId: contract.Id,
                        cancellationToken: cancellationToken);

                    var tenant = await _identityUserReader.GetUserByIdAsync(contract.TenantId, cancellationToken);
                    if (tenant is not null && !string.IsNullOrWhiteSpace(tenant.Email))
                    {
                        await _leasingEmailService.SendContractExpiringReminderEmailAsync(
                            tenant.Email, tenant.FullName, contract.ContractCode, contract.EndDate, daysRemaining, cancellationToken);
                    }
                }

                // Cập nhật mốc thời gian đã gửi nhắc nhở để chống gửi lặp lại trong cùng ngày
                contract.MarkExpirationReminderSent(now);
                await _contractRepository.SaveChangesAsync(cancellationToken);
                sentCount++;

                _logger.LogInformation(
                    "Đã gửi nhắc nhở hết hạn thành công cho hợp đồng #{ContractCode} [ID: {ContractId}, Còn: {Days} ngày].",
                    contract.ContractCode, contract.Id, daysRemaining);
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex,
                    "Lỗi khi gửi nhắc nhở hết hạn cho hợp đồng #{ContractCode} [ID: {ContractId}]: {Message}",
                    contract.ContractCode, contract.Id, ex.Message);
            }
        }

        _logger.LogInformation(
            "Hoàn tất gửi nhắc nhở hết hạn hợp đồng. Ứng viên: {Candidates}, Đã gửi: {Sent}, Bỏ qua: {Skipped}, Lỗi: {Errors}",
            totalCandidates, sentCount, skippedCount, errorCount);

        return Result.Success(new SendContractExpirationRemindersResult(
            totalCandidates, sentCount, skippedCount, errorCount));
    }
}
