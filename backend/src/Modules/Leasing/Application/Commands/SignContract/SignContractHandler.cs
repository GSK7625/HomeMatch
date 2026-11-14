using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.SignContract;

public sealed class SignContractHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdentityOtpService _identityOtpService;
    private readonly IIdentityUserReader _identityUserReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILeasingEmailService _leasingEmailService;
    private readonly ILogger<SignContractHandler> _logger;

    public SignContractHandler(
        IContractRepository contractRepository,
        IIdentityOtpService identityOtpService,
        IIdentityUserReader identityUserReader,
        INotificationPublisher notificationPublisher,
        ILeasingEmailService leasingEmailService,
        ILogger<SignContractHandler> logger)
    {
        _contractRepository = contractRepository;
        _identityOtpService = identityOtpService;
        _identityUserReader = identityUserReader;
        _notificationPublisher = notificationPublisher;
        _leasingEmailService = leasingEmailService;
        _logger = logger;
    }

    public async Task<Result<ContractDto>> HandleAsync(
        SignContractCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra bằng chứng chữ ký (document hash)
        if (string.IsNullOrWhiteSpace(command.DocumentHash))
        {
            return Result.Failure<ContractDto>(LeasingErrors.SignatureEvidenceRequired);
        }

        // 2. Kiểm tra mã OTP
        if (string.IsNullOrWhiteSpace(command.OtpCode))
        {
            return Result.Failure<ContractDto>(LeasingErrors.InvalidOtp);
        }

        // 3. Xác thực OTP thông qua ASP.NET Core Identity Token Provider
        var isOtpValid = await _identityOtpService.VerifySignatureOtpAsync(
            command.ActorId,
            command.ContractId,
            command.OtpCode,
            cancellationToken);

        if (!isOtpValid)
        {
            _logger.LogWarning("Xác thực OTP thất bại cho Người dùng [ID: {ActorId}] trên Hợp đồng [ID: {ContractId}].",
                command.ActorId, command.ContractId);
            return Result.Failure<ContractDto>(LeasingErrors.InvalidOtp);
        }

        // 4. Khóa dòng hợp đồng với SELECT ... FOR UPDATE để bảo vệ an toàn tuyệt đối khi cả 2 bên ký đồng thời
        var contract = await _contractRepository.GetForUpdateAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDto>(LeasingErrors.NotFound);
        }

        var now = DateTimeOffset.UtcNow;
        Result signResult;

        if (command.ActorId == contract.LandlordId)
        {
            signResult = contract.SignByLandlord(command.DocumentHash, now);
        }
        else if (command.ActorId == contract.TenantId)
        {
            signResult = contract.SignByTenant(command.DocumentHash, now);
        }
        else
        {
            return Result.Failure<ContractDto>(LeasingErrors.UnauthorizedSigner);
        }

        if (signResult.IsFailure)
        {
            return Result.Failure<ContractDto>(signResult.Error);
        }

        // 5. Lưu trạng thái vào Database
        await _contractRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Người dùng [ID: {ActorId}] đã ký xác thực OTP thành công trên hợp đồng [ID: {ContractId}]. Trạng thái hiện tại: {Status}",
            command.ActorId, contract.Id, contract.Status);

        // 6. Nếu cả hai bên đều đã ký xong (chuyển sang SIGNED) -> Gửi email & notification cho cả Chủ nhà và Người thuê
        if (contract.Status == ContractStatus.Signed)
        {
            try
            {
                var landlord = await _identityUserReader.GetUserByIdAsync(contract.LandlordId, cancellationToken);
                var tenant = await _identityUserReader.GetUserByIdAsync(contract.TenantId, cancellationToken);

                if (landlord is not null && !string.IsNullOrWhiteSpace(landlord.Email))
                {
                    await _leasingEmailService.SendContractSignedNotificationAsync(
                        landlord.Email, landlord.FullName, contract.ContractCode, cancellationToken);
                }

                if (tenant is not null && !string.IsNullOrWhiteSpace(tenant.Email))
                {
                    await _leasingEmailService.SendContractSignedNotificationAsync(
                        tenant.Email, tenant.FullName, contract.ContractCode, cancellationToken);
                }

                await _notificationPublisher.PublishAsync(
                    userId: contract.LandlordId,
                    title: "Hợp đồng đã hoàn tất ký",
                    content: $"Hợp đồng thuê #{contract.ContractCode} đã được cả hai bên ký thành công và chuyển sang SIGNED.",
                    type: NotificationTypeConstants.ContractSigned,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);

                await _notificationPublisher.PublishAsync(
                    userId: contract.TenantId,
                    title: "Hợp đồng đã hoàn tất ký",
                    content: $"Hợp đồng thuê #{contract.ContractCode} đã được cả hai bên ký thành công và chuyển sang SIGNED.",
                    type: NotificationTypeConstants.ContractSigned,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo hoàn tất ký hợp đồng [ID: {ContractId}]", contract.Id);
            }
        }

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
