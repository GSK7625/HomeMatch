using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.ProcessContractLifecycle;

public sealed class ProcessContractLifecycleHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdentityUserReader _identityUserReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILeasingEmailService _leasingEmailService;
    private readonly ILogger<ProcessContractLifecycleHandler> _logger;

    public ProcessContractLifecycleHandler(
        IContractRepository contractRepository,
        IIdentityUserReader identityUserReader,
        INotificationPublisher notificationPublisher,
        ILeasingEmailService leasingEmailService,
        ILogger<ProcessContractLifecycleHandler> logger)
    {
        _contractRepository = contractRepository;
        _identityUserReader = identityUserReader;
        _notificationPublisher = notificationPublisher;
        _leasingEmailService = leasingEmailService;
        _logger = logger;
    }

    public async Task<Result<ProcessContractLifecycleResult>> HandleAsync(
        ProcessContractLifecycleCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = command.Now ?? DateTimeOffset.UtcNow;
        var today = command.Today ?? DateOnly.FromDateTime(now.Date);

        _logger.LogInformation(
            "Bắt đầu xử lý vòng đời hợp đồng tự động tại ngày {Today} ({Now:yyyy-MM-dd HH:mm:ss} UTC).",
            today, now);

        var activatedCount = await ProcessSignedToActiveAsync(today, cancellationToken);
        var expiredPendingHandoverCount = await ProcessActiveToEndOfTermAsync(today, cancellationToken);
        var handoverOverdueCount = await ProcessHandoverOverdueAsync(today, cancellationToken);

        var totalProcessed = activatedCount.Processed + expiredPendingHandoverCount.Processed + handoverOverdueCount.Processed;
        var totalErrors = activatedCount.Errors + expiredPendingHandoverCount.Errors + handoverOverdueCount.Errors;

        _logger.LogInformation(
            "Hoàn tất xử lý vòng đời hợp đồng. Kích hoạt ACTIVE: {Activated}, Chờ bàn giao: {PendingHandover}, Quá hạn bàn giao: {Overdue}, Lỗi: {Errors}",
            activatedCount.Success, expiredPendingHandoverCount.Success, handoverOverdueCount.Success, totalErrors);

        return Result.Success(new ProcessContractLifecycleResult(
            activatedCount.Success,
            expiredPendingHandoverCount.Success,
            handoverOverdueCount.Success,
            totalProcessed,
            totalErrors));
    }

    // 1. Kích hoạt hợp đồng khi đến ngày bắt đầu thuê (SIGNED -> ACTIVE)
    private async Task<(int Processed, int Success, int Errors)> ProcessSignedToActiveAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var contracts = await _contractRepository.GetSignedContractsReadyForActivationAsync(today, cancellationToken);
        var processed = contracts.Count;
        var success = 0;
        var errors = 0;

        foreach (var contract in contracts)
        {
            try
            {
                var result = contract.Activate(today);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Không thể kích hoạt hợp đồng #{ContractCode} [ID: {ContractId}]: {Error}",
                        contract.ContractCode, contract.Id, result.Error.Message);
                    continue;
                }

                await _contractRepository.SaveChangesAsync(cancellationToken);
                success++;

                _logger.LogInformation(
                    "Hợp đồng #{ContractCode} [ID: {ContractId}] đã được kích hoạt ACTIVE từ ngày {StartDate}.",
                    contract.ContractCode, contract.Id, contract.StartDate);

                // Gửi thông báo trong app
                await _notificationPublisher.PublishAsync(
                    userId: contract.LandlordId,
                    title: "Hợp đồng thuê đã chính thức có hiệu lực",
                    content: $"Hợp đồng thuê #{contract.ContractCode} đã đến ngày bắt đầu ({contract.StartDate:dd/MM/yyyy}) và chuyển sang trạng thái ACTIVE.",
                    type: NotificationTypeConstants.ContractActive,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                await _notificationPublisher.PublishAsync(
                    userId: contract.TenantId,
                    title: "Hợp đồng thuê đã chính thức có hiệu lực",
                    content: $"Hợp đồng thuê #{contract.ContractCode} đã chính thức có hiệu lực từ ngày hôm nay ({contract.StartDate:dd/MM/yyyy}).",
                    type: NotificationTypeConstants.ContractActive,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex,
                    "Lỗi khi kích hoạt hợp đồng #{ContractCode} [ID: {ContractId}]: {Message}",
                    contract.ContractCode, contract.Id, ex.Message);
            }
        }

        return (processed, success, errors);
    }

    // 2. Chuyển hợp đồng hết hạn thuê sang chờ bàn giao phòng (ACTIVE -> EXPIRED_PENDING_HANDOVER)
    private async Task<(int Processed, int Success, int Errors)> ProcessActiveToEndOfTermAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var contracts = await _contractRepository.GetActiveContractsPastEndDateAsync(today, cancellationToken);
        var processed = contracts.Count;
        var success = 0;
        var errors = 0;

        foreach (var contract in contracts)
        {
            try
            {
                var result = contract.MarkExpiredPendingHandover(today);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Không thể chuyển hợp đồng #{ContractCode} [ID: {ContractId}] sang EXPIRED_PENDING_HANDOVER: {Error}",
                        contract.ContractCode, contract.Id, result.Error.Message);
                    continue;
                }

                await _contractRepository.SaveChangesAsync(cancellationToken);
                success++;

                _logger.LogInformation(
                    "Hợp đồng #{ContractCode} [ID: {ContractId}] đã chuyển sang EXPIRED_PENDING_HANDOVER sau ngày kết thúc {EndDate}.",
                    contract.ContractCode, contract.Id, contract.EndDate);

                // Gửi thông báo trong app
                await _notificationPublisher.PublishAsync(
                    userId: contract.LandlordId,
                    title: "Hợp đồng thuê đã kết thúc kỳ hạn",
                    content: $"Hợp đồng #{contract.ContractCode} đã kết thúc thời gian thuê vào ngày {contract.EndDate:dd/MM/yyyy}. Vui lòng tiến hành nghiệm thu và xác nhận bàn giao phòng.",
                    type: NotificationTypeConstants.ContractExpiredPendingHandover,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                await _notificationPublisher.PublishAsync(
                    userId: contract.TenantId,
                    title: "Hợp đồng thuê đã kết thúc kỳ hạn",
                    content: $"Hợp đồng #{contract.ContractCode} đã hết hạn thuê. Vui lòng bàn giao phòng và hoàn trả mặt bằng cho chủ nhà.",
                    type: NotificationTypeConstants.ContractExpiredPendingHandover,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                // Gửi email
                var landlord = await _identityUserReader.GetUserByIdAsync(contract.LandlordId, cancellationToken);
                if (landlord is not null && !string.IsNullOrWhiteSpace(landlord.Email))
                {
                    await _leasingEmailService.SendContractExpiredPendingHandoverEmailAsync(
                        landlord.Email, landlord.FullName, contract.ContractCode, contract.HandoverDueAt, cancellationToken);
                }

                var tenant = await _identityUserReader.GetUserByIdAsync(contract.TenantId, cancellationToken);
                if (tenant is not null && !string.IsNullOrWhiteSpace(tenant.Email))
                {
                    await _leasingEmailService.SendContractExpiredPendingHandoverEmailAsync(
                        tenant.Email, tenant.FullName, contract.ContractCode, contract.HandoverDueAt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex,
                    "Lỗi khi xử lý hết hạn hợp đồng #{ContractCode} [ID: {ContractId}]: {Message}",
                    contract.ContractCode, contract.Id, ex.Message);
            }
        }

        return (processed, success, errors);
    }

    // 3. Chuyển hợp đồng sang quá hạn bàn giao (EXPIRED_PENDING_HANDOVER -> HANDOVER_OVERDUE)
    private async Task<(int Processed, int Success, int Errors)> ProcessHandoverOverdueAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var contracts = await _contractRepository.GetExpiredContractsPastHandoverDueAsync(today, cancellationToken);
        var processed = contracts.Count;
        var success = 0;
        var errors = 0;

        foreach (var contract in contracts)
        {
            try
            {
                var result = contract.MarkHandoverOverdue(today);
                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Không thể chuyển hợp đồng #{ContractCode} [ID: {ContractId}] sang HANDOVER_OVERDUE: {Error}",
                        contract.ContractCode, contract.Id, result.Error.Message);
                    continue;
                }

                await _contractRepository.SaveChangesAsync(cancellationToken);
                success++;

                _logger.LogWarning(
                    "Hợp đồng #{ContractCode} [ID: {ContractId}] đã chuyển sang HANDOVER_OVERDUE.",
                    contract.ContractCode, contract.Id);

                // Gửi thông báo trong app
                await _notificationPublisher.PublishAsync(
                    userId: contract.LandlordId,
                    title: "Hợp đồng đã QUÁ HẠN bàn giao phòng",
                    content: $"Hợp đồng #{contract.ContractCode} đã vượt quá hạn chót bàn giao phòng ({contract.HandoverDueAt:dd/MM/yyyy HH:mm} UTC).",
                    type: NotificationTypeConstants.ContractHandoverOverdue,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                await _notificationPublisher.PublishAsync(
                    userId: contract.TenantId,
                    title: "CẢNH BÁO: Quá hạn bàn giao phòng",
                    content: $"Bạn đã quá hạn bàn giao phòng cho hợp đồng #{contract.ContractCode}. Vui lòng liên hệ chủ nhà ngay.",
                    type: NotificationTypeConstants.ContractHandoverOverdue,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                // Gửi email
                var landlord = await _identityUserReader.GetUserByIdAsync(contract.LandlordId, cancellationToken);
                if (landlord is not null && !string.IsNullOrWhiteSpace(landlord.Email))
                {
                    await _leasingEmailService.SendHandoverOverdueEmailAsync(
                        landlord.Email, landlord.FullName, contract.ContractCode, cancellationToken);
                }

                var tenant = await _identityUserReader.GetUserByIdAsync(contract.TenantId, cancellationToken);
                if (tenant is not null && !string.IsNullOrWhiteSpace(tenant.Email))
                {
                    await _leasingEmailService.SendHandoverOverdueEmailAsync(
                        tenant.Email, tenant.FullName, contract.ContractCode, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex,
                    "Lỗi khi xử lý quá hạn bàn giao hợp đồng #{ContractCode} [ID: {ContractId}]: {Message}",
                    contract.ContractCode, contract.Id, ex.Message);
            }
        }

        return (processed, success, errors);
    }
}
