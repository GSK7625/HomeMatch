using HomeMatch.Modules.Viewings.Domain.Entities;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Viewings.Application.DTOs;

internal static class ViewingAppointmentMapper
{
    public static ViewingAppointmentDto ToDto(
        ViewingAppointment appointment,
        ListingContractInfo? listing,
        IdentityUserInfo? tenant,
        IdentityUserInfo? landlord)
    {
        VirtualViewingDto? virtualViewingDto = null;
        if (appointment.VirtualViewing is not null)
        {
            virtualViewingDto = new VirtualViewingDto(
                Id: appointment.VirtualViewing.Id,
                RoomUrl: appointment.VirtualViewing.RoomUrl,
                Status: appointment.VirtualViewing.Status,
                StartedAt: appointment.VirtualViewing.StartedAt,
                EndedAt: appointment.VirtualViewing.EndedAt,
                CreatedAt: appointment.VirtualViewing.CreatedAt);
        }

        return new ViewingAppointmentDto(
            Id: appointment.Id,
            TenantId: appointment.TenantId,
            TenantName: tenant?.FullName ?? $"Người thuê #{appointment.TenantId}",
            TenantPhone: tenant?.PhoneNumber,
            ListingId: appointment.ListingId,
            ListingTitle: listing?.Title ?? $"Tin đăng #{appointment.ListingId}",
            ListingPrice: listing?.Price ?? 0m,
            ListingAddress: listing?.Address,
            ListingPrimaryImageUrl: listing?.PrimaryImageUrl,
            LandlordId: listing?.LandlordId ?? 0,
            LandlordName: landlord?.FullName ?? "Chủ nhà",
            LandlordPhone: landlord?.PhoneNumber,
            AppointmentTime: appointment.AppointmentTime,
            DurationMinutes: appointment.DurationMinutes,
            Notes: appointment.Notes,
            Status: appointment.Status,
            CancelledBy: appointment.CancelledBy,
            CancelledReason: appointment.CancelledReason,
            RemindAt: appointment.RemindAt,
            IsReminded: appointment.IsReminded,
            VirtualViewing: virtualViewingDto,
            CreatedAt: appointment.CreatedAt,
            UpdatedAt: appointment.UpdatedAt);
    }
}
