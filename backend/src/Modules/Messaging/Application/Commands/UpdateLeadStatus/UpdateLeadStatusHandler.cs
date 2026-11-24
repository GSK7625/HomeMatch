using HomeMatch.Modules.Messaging.Application.DTOs;
using HomeMatch.Modules.Messaging.Application.Interfaces;
using HomeMatch.Modules.Messaging.Domain.Constants;
using HomeMatch.Modules.Messaging.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Properties;

namespace HomeMatch.Modules.Messaging.Application.Commands.UpdateLeadStatus;

public sealed class UpdateLeadStatusHandler
{
    private readonly ILeadRepository _leadRepository;
    private readonly IListingDashboardReader _listingReader;
    private readonly IPropertyDashboardReader _propertyReader;
    private readonly IIdentityUserReader _userReader;

    public UpdateLeadStatusHandler(
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

    public async Task<Result<LandlordLeadDto>> HandleAsync(
        UpdateLeadStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(command.LeadId, cancellationToken);
        if (lead is null)
        {
            return Result.Failure<LandlordLeadDto>(MessagingErrors.LeadNotFound);
        }

        var landlordListings = await _listingReader.GetListingsForDashboardAsync(
            command.LandlordId,
            roomIds: null,
            cancellationToken);

        var listing = landlordListings.FirstOrDefault(l => l.ListingId == lead.ListingId);
        if (listing is null)
        {
            return Result.Failure<LandlordLeadDto>(MessagingErrors.LeadForbidden);
        }

        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            var normalizedStatus = command.Status.Trim().ToUpperInvariant();
            if (!LeadStatus.All.Contains(normalizedStatus))
            {
                return Result.Failure<LandlordLeadDto>(MessagingErrors.InvalidLeadStatus);
            }
            lead.Status = normalizedStatus;
        }

        if (command.UpdateNotes)
        {
            var trimmedNotes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
            lead.Notes = trimmedNotes;
        }

        await _leadRepository.SaveChangesAsync(cancellationToken);

        var rooms = await _propertyReader.GetRoomsForDashboardAsync(
            command.LandlordId,
            propertyId: null,
            cancellationToken);
        var room = rooms.FirstOrDefault(r => r.RoomId == listing.RoomId);

        var tenant = await _userReader.GetUserByIdAsync(lead.TenantId, cancellationToken);
        var conversationMap = await _leadRepository.GetConversationMapByListingIdsAsync(
            [lead.ListingId],
            command.LandlordId,
            cancellationToken);
        conversationMap.TryGetValue((lead.TenantId, lead.ListingId), out var conversationId);

        return Result.Success(new LandlordLeadDto(
            Id: lead.Id,
            TenantId: lead.TenantId,
            TenantName: tenant?.FullName ?? $"Khách thuê #{lead.TenantId}",
            TenantPhone: tenant?.PhoneNumber,
            TenantEmail: tenant?.Email,
            ListingId: lead.ListingId,
            ListingTitle: listing.Title,
            RoomId: listing.RoomId,
            RoomNumber: room?.RoomNumber,
            PropertyId: room?.PropertyId,
            PropertyName: room?.PropertyName,
            Status: lead.Status,
            Source: lead.Source,
            Notes: lead.Notes,
            ConversationId: conversationId > 0 ? conversationId : null,
            CreatedAt: lead.CreatedAt));
    }
}
