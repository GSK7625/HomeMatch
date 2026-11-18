using HomeMatch.Modules.Billing.Application.DTOs;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints.Responses;

public static class InvoiceResponseMapper
{
    public static InvoiceResponse ToResponse(InvoiceDto dto) =>
        new(
            dto.Id,
            dto.InvoiceCode,
            dto.ContractId,
            dto.LandlordId,
            dto.TenantId,
            dto.RoomId,
            dto.PropertyId,
            dto.BillingMonth,
            dto.BillingYear,
            dto.RentAmount,
            dto.ElectricityAmount,
            dto.WaterAmount,
            dto.ServiceAmount,
            dto.DiscountAmount,
            dto.TotalAmount,
            dto.PaidAmount,
            dto.DueDate,
            dto.PaidAt,
            dto.Status,
            dto.CancelReason,
            dto.CancelledAt,
            dto.CreatedAt,
            dto.UpdatedAt);

    public static IReadOnlyList<InvoiceResponse> ToResponse(IReadOnlyList<InvoiceDto> dtos) =>
        dtos.Select(ToResponse).ToList();

    public static PagedInvoicesResponse ToResponse(PagedInvoicesDto dto) =>
        new(
            ToResponse(dto.Items),
            dto.Page,
            dto.PageSize,
            dto.TotalCount);

    public static InvoiceDetailResponse ToResponse(InvoiceDetailDto dto) =>
        new(
            dto.Id,
            dto.InvoiceCode,
            dto.ContractId,
            dto.ContractCode,
            dto.LandlordId,
            dto.TenantId,
            dto.RoomId,
            dto.PropertyId,
            dto.BillingMonth,
            dto.BillingYear,
            dto.RentAmount,
            dto.ElectricityAmount,
            dto.WaterAmount,
            dto.ServiceAmount,
            dto.DiscountAmount,
            dto.TotalAmount,
            dto.PaidAmount,
            dto.DueDate,
            dto.PaidAt,
            dto.Status,
            dto.CancelReason,
            dto.CancelledAt,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.Payments.Select(ToResponse).ToList());

    public static PaymentResponse ToResponse(PaymentDto dto) =>
        new(
            dto.Id,
            dto.PaymentCode,
            dto.InvoiceId,
            dto.Amount,
            dto.PaymentMethod,
            dto.GatewayTransactionNo,
            dto.BankCode,
            dto.CardType,
            dto.GatewayResponseCode,
            dto.PaymentUrl,
            dto.Status,
            dto.ExpiredAt,
            dto.CompletedAt,
            dto.CreatedAt);
}
