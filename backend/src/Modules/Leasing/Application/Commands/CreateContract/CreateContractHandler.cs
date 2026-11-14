using System.Security.Cryptography;
using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Commands.CreateContract;

public sealed class CreateContractHandler
{
    private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB
    private readonly IContractRepository _contractRepository;
    private readonly IRoomListingReader _roomReader;
    private readonly IIdentityUserReader _userReader;
    private readonly IContractDocumentStorage _storage;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<CreateContractHandler> _logger;

    public CreateContractHandler(
        IContractRepository contractRepository,
        IRoomListingReader roomReader,
        IIdentityUserReader userReader,
        IContractDocumentStorage storage,
        INotificationPublisher notificationPublisher,
        ILogger<CreateContractHandler> logger)
    {
        _contractRepository = contractRepository;
        _roomReader = roomReader;
        _userReader = userReader;
        _storage = storage;
        _notificationPublisher = notificationPublisher;
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
            command.BillingDueDay);

        if (contractResult.IsFailure)
        {
            return Result.Failure<ContractDto>(contractResult.Error);
        }

        var contract = contractResult.Value;
        await _contractRepository.AddAsync(contract, cancellationToken);
        await _contractRepository.SaveChangesAsync(cancellationToken);

        // 6. Nếu Chủ nhà đính kèm file PDF hợp đồng xuất từ màn hình xem trước
        if (command.FileStream is not null && command.FileLength > 0)
        {
            if (command.FileLength > MaxFileSizeBytes)
            {
                return Result.Failure<ContractDto>(new Error("Leasing.FileTooLarge", "Kích thước tệp hợp đồng vượt quá giới hạn 15MB."));
            }

            var extension = Path.GetExtension(command.FileName)?.ToLowerInvariant();
            if (extension != ".pdf")
            {
                return Result.Failure<ContractDto>(new Error("Leasing.InvalidFileType", "Chỉ chấp nhận tài liệu định dạng PDF (.pdf)."));
            }

            using var memoryStream = new MemoryStream();
            await command.FileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            var hashBytes = SHA256.HashData(fileBytes);
            var documentHash = Convert.ToHexStringLower(hashBytes);

            var objectKey = $"contracts/{contract.Id}/v{contract.DocumentVersion}/{contract.ContractCode}.pdf";
            memoryStream.Position = 0;
            var storageKey = await _storage.UploadAsync(objectKey, memoryStream, "application/pdf", cancellationToken);

            var expiresAt = (command.SignatureExpiresAt ?? DateTimeOffset.UtcNow.AddDays(3)).ToUniversalTime();
            var submitResult = contract.SubmitForSignature(
                storageKey,
                documentHash,
                command.DocumentSnapshot,
                expiresAt,
                command.HandoverDueAt?.ToUniversalTime());

            if (submitResult.IsFailure)
            {
                return Result.Failure<ContractDto>(submitResult.Error);
            }

            await _contractRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã upload PDF hợp đồng [ID: {ContractId}] lên Cloudflare R2, Hash=[{Hash}], Key=[{Key}] và chuyển sang PENDING_SIGNATURE.",
                contract.Id, documentHash, storageKey);

            try
            {
                await _notificationPublisher.PublishAsync(
                    userId: contract.TenantId,
                    title: "Hợp đồng thuê mới chờ bạn ký",
                    content: $"Chủ nhà đã tạo hợp đồng thuê #{contract.ContractCode} kèm văn bản hoàn chỉnh để bạn xem và ký xác thực.",
                    type: NotificationTypeConstants.ContractPendingSignature,
                    referenceType: "CONTRACT",
                    referenceId: contract.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo hợp đồng mới #{ContractCode} cho người thuê", contract.ContractCode);
            }
        }
        else
        {
            _logger.LogInformation("Đã tạo hợp đồng mới thành công ở trạng thái DRAFT [ID: {ContractId}, Code: {Code}, Room: {RoomId}]",
                contract.Id, contract.ContractCode, contract.RoomId);
        }

        return Result.Success(ContractMapper.ToDto(contract));
    }
}
