using HomeMatch.Modules.Leasing.Application.Interfaces;
using HomeMatch.Modules.Leasing.Domain.Constants;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Leasing.Application.Queries.GetContractDocumentUrl;

public sealed class GetContractDocumentHandler
{
    private static readonly TimeSpan DownloadExpiry = TimeSpan.FromMinutes(30);
    private readonly IContractRepository _contractRepository;
    private readonly IContractDocumentStorage _storage;
    private readonly ILogger<GetContractDocumentHandler> _logger;

    public GetContractDocumentHandler(
        IContractRepository contractRepository,
        IContractDocumentStorage storage,
        ILogger<GetContractDocumentHandler> logger)
    {
        _contractRepository = contractRepository;
        _storage = storage;
        _logger = logger;
    }

    public async Task<Result<ContractDocumentUrlDto>> HandleAsync(
        GetContractDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        var contract = await _contractRepository.GetByIdAsync(query.ContractId, cancellationToken);
        if (contract is null)
        {
            return Result.Failure<ContractDocumentUrlDto>(LeasingErrors.NotFound);
        }

        if (contract.LandlordId != query.ActorId && contract.TenantId != query.ActorId)
        {
            return Result.Failure<ContractDocumentUrlDto>(LeasingErrors.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(contract.ContractFileUrl))
        {
            return Result.Failure<ContractDocumentUrlDto>(new Error(
                "Leasing.DocumentNotUploadedYet",
                "Hợp đồng này chưa có tài liệu PDF được lưu trữ."));
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(DownloadExpiry);
        var presignedUrl = await _storage.GetPresignedDownloadUrlAsync(
            contract.ContractFileUrl,
            DownloadExpiry,
            cancellationToken);

        _logger.LogInformation("Đã cấp URL tải tài liệu hợp đồng [ID: {ContractId}] cho User [ID: {ActorId}].",
            contract.Id, query.ActorId);

        return Result.Success(new ContractDocumentUrlDto(
            DownloadUrl: presignedUrl,
            DocumentHash: contract.SignedDocumentHash,
            ObjectKey: contract.ContractFileUrl,
            ExpiresAt: expiresAt));
    }
}
