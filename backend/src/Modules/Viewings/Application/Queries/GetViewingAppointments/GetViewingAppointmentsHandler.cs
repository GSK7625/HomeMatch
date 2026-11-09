using HomeMatch.Modules.Viewings.Application.DTOs;
using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointments;

public sealed class GetViewingAppointmentsHandler
{
    private readonly IViewingAppointmentRepository _appointmentRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;

    public GetViewingAppointmentsHandler(
        IViewingAppointmentRepository appointmentRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader)
    {
        _appointmentRepository = appointmentRepository;
        _listingReader = listingReader;
        _userReader = userReader;
    }

    public async Task<Result<PagedViewingAppointmentsDto>> HandleAsync(
        GetViewingAppointmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var isLandlordView = string.Equals(query.RoleView, "LANDLORD", StringComparison.OrdinalIgnoreCase);

        IReadOnlyList<Domain.Entities.ViewingAppointment> items;
        int totalCount;

        if (isLandlordView)
        {
            var listingIds = await _listingReader.GetListingIdsByLandlordIdAsync(query.UserId, cancellationToken);
            (items, totalCount) = await _appointmentRepository.GetPagedByListingIdsAsync(
                listingIds,
                query.Status,
                page,
                pageSize,
                cancellationToken);
        }
        else
        {
            (items, totalCount) = await _appointmentRepository.GetPagedByTenantAsync(
                query.UserId,
                query.Status,
                page,
                pageSize,
                cancellationToken);
        }

        if (items.Count == 0)
        {
            return Result.Success(new PagedViewingAppointmentsDto([], page, pageSize, totalCount));
        }

        var listingMap = await _listingReader.GetListingsByIdsAsync(
            items.Select(i => i.ListingId),
            cancellationToken);

        var userCache = new Dictionary<long, IdentityUserInfo?>();
        async Task<IdentityUserInfo?> GetUserCachedAsync(long userId)
        {
            if (userId <= 0) return null;
            if (userCache.TryGetValue(userId, out var cached)) return cached;
            var userInfo = await _userReader.GetUserByIdAsync(userId, cancellationToken);
            userCache[userId] = userInfo;
            return userInfo;
        }

        var dtos = new List<ViewingAppointmentDto>(items.Count);
        foreach (var item in items)
        {
            listingMap.TryGetValue(item.ListingId, out var listing);
            var tenant = await GetUserCachedAsync(item.TenantId);
            var landlord = listing is not null ? await GetUserCachedAsync(listing.LandlordId) : null;

            dtos.Add(ViewingAppointmentMapper.ToDto(item, listing, tenant, landlord));
        }

        return Result.Success(new PagedViewingAppointmentsDto(dtos, page, pageSize, totalCount));
    }
}
