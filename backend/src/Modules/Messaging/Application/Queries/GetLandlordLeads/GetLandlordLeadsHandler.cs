using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Properties;

namespace HomeMatch.Modules.Messaging.Application.Queries.GetLandlordLeads;

public sealed class GetLandlordLeadsHandler
{
    private readonly ILeadRepository _leadRepository;
    private readonly IListingDashboardReader _listingReader;
    private readonly IPropertyDashboardReader _propertyReader;
    private readonly IIdentityUserReader _userReader;

    public GetLandlordLeadsHandler(
        ILeadRepository leadRepository,
        IListingDashboardReader listingReader,
        IPropertyDashboardReader propertyReader,
        IIdentityUserReader userReader)
    {
        _leadRepository = leadRepository;
        _listingReader = listingReader;
        _propertyReader = propertyReader;
        _userReader = userReader;
    }

    public async Task<Result<LandlordLeadsResponseDto>> HandleAsync(
        GetLandlordLeadsQuery query,
        CancellationToken cancellationToken = default)
    {
        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            normalizedStatus = query.Status.Trim().ToUpperInvariant();
            if (normalizedStatus != "ALL" && !LeadStatus.All.Contains(normalizedStatus))
            {
                return Result.Failure<LandlordLeadsResponseDto>(MessagingErrors.InvalidLeadStatus);
            }
            if (normalizedStatus == "ALL")
            {
                normalizedStatus = null;
            }
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var listings = await _listingReader.GetListingsForDashboardAsync(
            query.LandlordId,
            roomIds: null,
            cancellationToken);

        var rooms = await _propertyReader.GetRoomsForDashboardAsync(
            query.LandlordId,
            propertyId: null,
            cancellationToken);

        var roomMap = rooms.ToDictionary(r => r.RoomId);
        var listingMap = listings.ToDictionary(l => l.ListingId);

        var listingOptions = listings
            .Select(l =>
            {
                roomMap.TryGetValue(l.RoomId, out var room);
                return new LandlordLeadFilterOptionDto(
                    ListingId: l.ListingId,
                    ListingTitle: l.Title,
                    RoomId: l.RoomId,
                    RoomNumber: room?.RoomNumber,
                    PropertyName: room?.PropertyName);
            })
            .OrderBy(o => o.PropertyName)
            .ThenBy(o => o.RoomNumber)
            .ThenBy(o => o.ListingTitle)
            .ToList();

        if (listings.Count == 0)
        {
            return Result.Success(new LandlordLeadsResponseDto(
                Items: [],
                Page: page,
                PageSize: pageSize,
                TotalCount: 0,
                Summary: new LandlordLeadStatusSummaryDto(0, 0, 0, 0, 0, 0),
                ListingOptions: listingOptions));
        }

        var allListingIds = listings.Select(l => l.ListingId).ToList();
        var allLeads = await _leadRepository.GetByListingIdsAsync(allListingIds, cancellationToken);

        // Summary counts reflect all leads of this landlord (optionally scoped by listing/room)
        var scopedForSummary = allLeads.AsEnumerable();
        if (query.ListingId.HasValue)
        {
            scopedForSummary = scopedForSummary.Where(l => l.ListingId == query.ListingId.Value);
        }
        if (query.RoomId.HasValue)
        {
            scopedForSummary = scopedForSummary.Where(l =>
                listingMap.TryGetValue(l.ListingId, out var info) && info.RoomId == query.RoomId.Value);
        }

        var summaryList = scopedForSummary.ToList();
        var summary = new LandlordLeadStatusSummaryDto(
            Total: summaryList.Count,
            NewCount: summaryList.Count(l => l.Status == LeadStatus.New),
            ContactedCount: summaryList.Count(l => l.Status == LeadStatus.Contacted),
            ViewingScheduledCount: summaryList.Count(l => l.Status == LeadStatus.ViewingScheduled),
            ContractSignedCount: summaryList.Count(l => l.Status == LeadStatus.ContractSigned),
            LostCount: summaryList.Count(l => l.Status == LeadStatus.Lost));

        var filteredLeads = summaryList.AsEnumerable();
        if (normalizedStatus is not null)
        {
            filteredLeads = filteredLeads.Where(l => l.Status == normalizedStatus);
        }

        var candidateLeads = filteredLeads.ToList();
        var conversationMap = await _leadRepository.GetConversationMapByListingIdsAsync(
            allListingIds,
            query.LandlordId,
            cancellationToken);

        var userCache = new Dictionary<long, IdentityUserInfo?>();
        async Task<IdentityUserInfo?> GetUserCachedAsync(long userId)
        {
            if (userId <= 0) return null;
            if (userCache.TryGetValue(userId, out var cached)) return cached;
            var info = await _userReader.GetUserByIdAsync(userId, cancellationToken);
            userCache[userId] = info;
            return info;
        }

        var enriched = new List<LandlordLeadDto>(candidateLeads.Count);
        foreach (var lead in candidateLeads)
        {
            listingMap.TryGetValue(lead.ListingId, out var listing);
            DashboardRoomInfo? room = null;
            if (listing is not null)
            {
                roomMap.TryGetValue(listing.RoomId, out room);
            }

            var tenant = await GetUserCachedAsync(lead.TenantId);
            conversationMap.TryGetValue((lead.TenantId, lead.ListingId), out var conversationId);

            enriched.Add(new LandlordLeadDto(
                Id: lead.Id,
                TenantId: lead.TenantId,
                TenantName: tenant?.FullName ?? $"Khách thuê #{lead.TenantId}",
                TenantPhone: tenant?.PhoneNumber,
                TenantEmail: tenant?.Email,
                ListingId: lead.ListingId,
                ListingTitle: listing?.Title ?? $"Tin đăng #{lead.ListingId}",
                RoomId: listing?.RoomId,
                RoomNumber: room?.RoomNumber,
                PropertyId: room?.PropertyId,
                PropertyName: room?.PropertyName,
                Status: lead.Status,
                Source: lead.Source,
                Notes: lead.Notes,
                ConversationId: conversationId > 0 ? conversationId : null,
                CreatedAt: lead.CreatedAt));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            enriched = enriched
                .Where(item =>
                    item.TenantName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(item.TenantPhone) && item.TenantPhone.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(item.TenantEmail) && item.TenantEmail.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || item.ListingTitle.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(item.RoomNumber) && item.RoomNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(item.PropertyName) && item.PropertyName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(item.Notes) && item.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var totalCount = enriched.Count;
        var pagedItems = enriched
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result.Success(new LandlordLeadsResponseDto(
            Items: pagedItems,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            Summary: summary,
            ListingOptions: listingOptions));
    }
}
