namespace HomeMatch.Modules.Billing.Application.DTOs;

/// <summary>
/// DTO đại diện cho 1 nhóm tuổi nợ theo chuẩn BRD Mục 6.4
/// </summary>
public sealed record DebtAgingGroupDto(
    string GroupCode,
    string GroupName,
    int InvoiceCount,
    long TotalAmount);
