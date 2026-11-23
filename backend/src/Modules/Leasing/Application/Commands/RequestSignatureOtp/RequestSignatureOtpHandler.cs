using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Leasing.Application.Commands.RequestSignatureOtp;

public sealed class RequestSignatureOtpHandler
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdentityUserReader _identityUserReader;
    private readonly IIdentityOtpService _identityOtpService;
    private readonly ILeasingEmailService _leasingEmailService;

    public RequestSignatureOtpHandler(
        IContractRepository _contractRepository,
        IIdentityUserReader identityUserReader,
        IIdentityOtpService identityOtpService,
        ILeasingEmailService leasingEmailService)
    {
        this._contractRepository = _contractRepository;
        _identityUserReader = identityUserReader;
        _identityOtpService = identityOtpService;
        _leasingEmailService = leasingEmailService;
    }

    public async Task<Result<RequestSignatureOtpResult>> HandleAsync(
        RequestSignatureOtpCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra tồn tại hợp đồng
        var contract = await _contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<RequestSignatureOtpResult>(LeasingErrors.NotFound);
        }

        // 2. Kiểm tra trạng thái phải là PENDING_SIGNATURE
        if (contract.Status != ContractStatus.PendingSignature)
        {
            return Result.Failure<RequestSignatureOtpResult>(
                LeasingErrors.InvalidStatusTransition(contract.Status.ToString(), ContractStatus.Signed.ToString()));
        }

        // 3. Kiểm tra thời hạn ký
        var now = DateTimeOffset.UtcNow;
        if (contract.SignatureExpiresAt.HasValue && now > contract.SignatureExpiresAt.Value)
        {
            return Result.Failure<RequestSignatureOtpResult>(LeasingErrors.SignatureExpired);
        }

        // 4. Kiểm tra quyền của Actor: phải là Chủ nhà hoặc Người thuê, và chưa ký
        var isLandlord = contract.LandlordId == command.ActorId;
        var isTenant = contract.TenantId == command.ActorId;

        if (!isLandlord && !isTenant)
        {
            return Result.Failure<RequestSignatureOtpResult>(LeasingErrors.UnauthorizedSigner);
        }

        if (isLandlord && contract.LandlordSignedAt.HasValue)
        {
            return Result.Failure<RequestSignatureOtpResult>(LeasingErrors.AlreadySignedByParty);
        }

        if (isTenant && contract.TenantSignedAt.HasValue)
        {
            return Result.Failure<RequestSignatureOtpResult>(LeasingErrors.AlreadySignedByParty);
        }

        // 5. Lấy email đáng tin cậy của User từ Identity
        var user = await _identityUserReader.GetUserByIdAsync(command.ActorId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return Result.Failure<RequestSignatureOtpResult>(LeasingErrors.UserEmailNotFound);
        }

        // 6. Sinh mã OTP 6 số từ Identity Token Provider
        var otp = await _identityOtpService.GenerateSignatureOtpAsync(command.ActorId, command.ContractId, cancellationToken);

        // 7. Gửi email thông báo chứa OTP
        await _leasingEmailService.SendSignatureOtpEmailAsync(
            toEmail: user.Email,
            recipientName: user.FullName,
            contractCode: contract.ContractCode,
            otp: otp,
            cancellationToken: cancellationToken);

        var expiresAt = now.AddMinutes(5);
        var maskedEmail = MaskEmail(user.Email);

        return Result.Success(new RequestSignatureOtpResult(
            MaskedEmail: maskedEmail,
            ExpiresAt: expiresAt,
            Message: $"Mã xác thực OTP đã được gửi đến email {maskedEmail}. Vui lòng kiểm tra hộp thư."));
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return email;

        var name = email[..atIndex];
        var domain = email[atIndex..];

        var visibleChar = name[0];
        return $"{visibleChar}***{domain}";
    }
}
