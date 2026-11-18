using HomeMatch.Modules.Leasing.Application.Commands.RequestSignatureOtp;
using HomeMatch.Modules.Leasing.Application.DTOs;
using HomeMatch.Modules.Leasing.Application.Queries.GetContractDocumentUrl;

namespace HomeMatch.Modules.Leasing.Presentation.Endpoints.Responses;

public static class ContractResponseMapper
{
    public static ContractResponse ToResponse(ContractDto dto) =>
        new(
            Id: dto.Id,
            ContractCode: dto.ContractCode,
            RoomId: dto.RoomId,
            TenantId: dto.TenantId,
            LandlordId: dto.LandlordId,
            StartDate: dto.StartDate,
            EndDate: dto.EndDate,
            MonthlyRent: dto.MonthlyRent,
            DepositAmount: dto.DepositAmount,
            DepositStatus: dto.DepositStatus,
            BillingDueDay: dto.BillingDueDay,
            ContractFileUrl: dto.ContractFileUrl,
            DocumentSnapshot: dto.DocumentSnapshot,
            DocumentVersion: dto.DocumentVersion,
            DocumentGeneratedAt: dto.DocumentGeneratedAt,
            Status: dto.Status,
            SignatureExpiresAt: dto.SignatureExpiresAt,
            LandlordSignedAt: dto.LandlordSignedAt,
            TenantSignedAt: dto.TenantSignedAt,
            SignedDocumentHash: dto.SignedDocumentHash,
            HandoverDueAt: dto.HandoverDueAt,
            HandoverConfirmedAt: dto.HandoverConfirmedAt,
            HandoverConfirmedBy: dto.HandoverConfirmedBy,
            ActualOccupancyEndDate: dto.ActualOccupancyEndDate,
            CreatedAt: dto.CreatedAt,
            UpdatedAt: dto.UpdatedAt);

    public static IReadOnlyList<ContractResponse> ToResponse(IReadOnlyList<ContractDto> dtos) =>
        dtos.Select(ToResponse).ToList();

    public static PagedContractsResponse ToResponse(PagedContractsDto dto) =>
        new(
            Items: ToResponse(dto.Items),
            Page: dto.Page,
            PageSize: dto.PageSize,
            TotalCount: dto.TotalCount);

    public static ContractDetailResponse ToResponse(ContractDetailDto dto) =>
        new(
            Id: dto.Id,
            ContractCode: dto.ContractCode,
            RoomId: dto.RoomId,
            TenantId: dto.TenantId,
            LandlordId: dto.LandlordId,
            StartDate: dto.StartDate,
            EndDate: dto.EndDate,
            MonthlyRent: dto.MonthlyRent,
            DepositAmount: dto.DepositAmount,
            DepositStatus: dto.DepositStatus,
            BillingDueDay: dto.BillingDueDay,
            ContractFileUrl: dto.ContractFileUrl,
            DocumentSnapshot: dto.DocumentSnapshot,
            DocumentVersion: dto.DocumentVersion,
            DocumentGeneratedAt: dto.DocumentGeneratedAt,
            Status: dto.Status,
            SignatureExpiresAt: dto.SignatureExpiresAt,
            LandlordSignedAt: dto.LandlordSignedAt,
            TenantSignedAt: dto.TenantSignedAt,
            SignedDocumentHash: dto.SignedDocumentHash,
            HandoverDueAt: dto.HandoverDueAt,
            HandoverConfirmedAt: dto.HandoverConfirmedAt,
            HandoverConfirmedBy: dto.HandoverConfirmedBy,
            ActualOccupancyEndDate: dto.ActualOccupancyEndDate,
            CreatedAt: dto.CreatedAt,
            UpdatedAt: dto.UpdatedAt,
            TotalDepositReceived: dto.TotalDepositReceived,
            TotalDepositRefunded: dto.TotalDepositRefunded,
            TotalDepositDeducted: dto.TotalDepositDeducted,
            CurrentDepositBalance: dto.CurrentDepositBalance,
            DepositTransactions: ToResponse(dto.DepositTransactions));

    public static DepositTransactionResponse ToResponse(DepositTransactionDto dto) =>
        new(
            Id: dto.Id,
            ContractId: dto.ContractId,
            TransactionType: dto.TransactionType,
            Amount: dto.Amount,
            Reason: dto.Reason,
            RecordedBy: dto.RecordedBy,
            CreatedAt: dto.CreatedAt);

    public static IReadOnlyList<DepositTransactionResponse> ToResponse(IReadOnlyList<DepositTransactionDto> dtos) =>
        dtos.Select(ToResponse).ToList();

    public static DepositBalanceResponse ToResponse(DepositBalanceDto dto) =>
        new(
            ContractId: dto.ContractId,
            AgreedDepositAmount: dto.AgreedDepositAmount,
            TotalReceived: dto.TotalReceived,
            TotalRefunded: dto.TotalRefunded,
            TotalDeducted: dto.TotalDeducted,
            RemainingBalance: dto.RemainingBalance,
            DepositStatus: dto.DepositStatus);

    public static DepositHistoryResponse ToResponse(DepositHistoryDto dto) =>
        new(
            ContractId: dto.ContractId,
            AgreedDepositAmount: dto.AgreedDepositAmount,
            TotalReceived: dto.TotalReceived,
            TotalRefunded: dto.TotalRefunded,
            TotalDeducted: dto.TotalDeducted,
            CurrentBalance: dto.CurrentBalance,
            Transactions: ToResponse(dto.Transactions));

    public static HandoverDetailsResponse ToResponse(HandoverDetailsDto dto) =>
        new(
            ContractId: dto.ContractId,
            ContractCode: dto.ContractCode,
            RoomId: dto.RoomId,
            ContractStatus: dto.ContractStatus,
            StartDate: dto.StartDate,
            EndDate: dto.EndDate,
            HandoverDueAt: dto.HandoverDueAt,
            IsHandoverConfirmed: dto.IsHandoverConfirmed,
            HandoverConfirmedAt: dto.HandoverConfirmedAt,
            HandoverConfirmedBy: dto.HandoverConfirmedBy,
            ActualOccupancyEndDate: dto.ActualOccupancyEndDate,
            IsOccupancyReleased: dto.IsOccupancyReleased);

    public static ContractDocumentUrlResponse ToResponse(ContractDocumentUrlDto dto) =>
        new(
            DownloadUrl: dto.DownloadUrl,
            DocumentHash: dto.DocumentHash,
            ObjectKey: dto.ObjectKey,
            ExpiresAt: dto.ExpiresAt);

    public static RequestSignatureOtpResponse ToResponse(RequestSignatureOtpResult result) =>
        new(
            MaskedEmail: result.MaskedEmail,
            ExpiresAt: result.ExpiresAt,
            Message: result.Message);
}
