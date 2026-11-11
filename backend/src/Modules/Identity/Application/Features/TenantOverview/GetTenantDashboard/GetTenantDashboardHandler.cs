using HomeMatch.Modules.Identity.Domain.Constants;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Messaging;
using HomeMatch.Shared.Contracts.Viewings;

namespace HomeMatch.Modules.Identity.Application.Features.TenantOverview.GetTenantDashboard;

public sealed class GetTenantDashboardHandler
{
    private readonly IIdentityUserReader _userReader;
    private readonly ITenantListingActivityReader _listingActivityReader;
    private readonly IViewingStatsReader _viewingStatsReader;
    private readonly IMessagingStatsReader _messagingStatsReader;

    public GetTenantDashboardHandler(
        IIdentityUserReader userReader,
        ITenantListingActivityReader listingActivityReader,
        IViewingStatsReader viewingStatsReader,
        IMessagingStatsReader messagingStatsReader)
    {
        _userReader = userReader;
        _listingActivityReader = listingActivityReader;
        _viewingStatsReader = viewingStatsReader;
        _messagingStatsReader = messagingStatsReader;
    }

    public async Task<Result<TenantDashboardSummaryDto>> HandleAsync(
        GetTenantDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var userInfo = await _userReader.GetUserByIdAsync(query.TenantId, cancellationToken);
        if (userInfo is null)
        {
            return Result.Failure<TenantDashboardSummaryDto>(
                new Error(IdentityErrors.AuthenticationRequired, "Không tìm thấy thông tin tài khoản người thuê."));
        }

        // Thực thi tuần tự bất đồng bộ để tuân thủ quy tắc Concurrency trong backend-rules.md
        var totalViewed = await _listingActivityReader.GetTotalViewedListingsCountAsync(query.TenantId, cancellationToken);
        var totalSaved = await _listingActivityReader.GetTotalSavedListingsCountAsync(query.TenantId, cancellationToken);
        var totalAppointments = await _viewingStatsReader.GetTotalAppointmentsCountByTenantAsync(query.TenantId, cancellationToken);
        var totalConversations = await _messagingStatsReader.GetTotalConversationsCountByUserAsync(query.TenantId, cancellationToken);

        return Result.Success(new TenantDashboardSummaryDto(
            TenantId: userInfo.Id,
            FullName: userInfo.FullName,
            Email: userInfo.Email,
            PhoneNumber: userInfo.PhoneNumber,
            TotalViewedListings: totalViewed,
            TotalSavedListings: totalSaved,
            TotalAppointments: totalAppointments,
            TotalConversations: totalConversations));
    }
}
