namespace HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetTenantDashboard;

public record GetTenantDashboardQuery(long TenantId);

public record TenantDashboardSummaryDto(
    long TenantId,
    string FullName,
    string Email,
    string? PhoneNumber,
    int TotalViewedListings,
    int TotalSavedListings,
    int TotalAppointments,
    int TotalConversations);
