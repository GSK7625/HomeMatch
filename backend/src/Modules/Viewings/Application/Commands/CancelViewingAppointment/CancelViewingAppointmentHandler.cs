using HomeMatch.Modules.Viewings.Application.DTOs;
using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Modules.Viewings.Domain.Constants;
using HomeMatch.Modules.Viewings.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Viewings.Application.Commands.CancelViewingAppointment;

public sealed class CancelViewingAppointmentHandler
{
    private readonly IViewingAppointmentRepository _appointmentRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<CancelViewingAppointmentHandler> _logger;

    public CancelViewingAppointmentHandler(
        IViewingAppointmentRepository appointmentRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader,
        INotificationPublisher notificationPublisher,
        ILogger<CancelViewingAppointmentHandler> logger)
    {
        _appointmentRepository = appointmentRepository;
        _listingReader = listingReader;
        _userReader = userReader;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result<ViewingAppointmentDto>> HandleAsync(
        CancelViewingAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.NotFound);
        }

        var listing = await _listingReader.GetListingByIdAsync(appointment.ListingId, cancellationToken);

        string cancelledBy;
        if (appointment.TenantId == command.ActorUserId)
        {
            cancelledBy = ViewingCancelledBy.Tenant;
        }
        else if (listing is not null && listing.LandlordId == command.ActorUserId)
        {
            cancelledBy = ViewingCancelledBy.Landlord;
        }
        else
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.Forbidden);
        }

        if (appointment.Status != ViewingAppointmentStatus.Pending
            && appointment.Status != ViewingAppointmentStatus.Confirmed)
        {
            return Result.Failure<ViewingAppointmentDto>(
                ViewingErrors.InvalidStatusTransition(appointment.Status, ViewingAppointmentStatus.Cancelled));
        }

        appointment.Status = ViewingAppointmentStatus.Cancelled;
        appointment.CancelledBy = cancelledBy;
        appointment.CancelledReason = string.IsNullOrWhiteSpace(command.CancelledReason)
            ? null
            : command.CancelledReason.Trim();
        appointment.UpdatedAt = DateTimeOffset.UtcNow;

        if (appointment.VirtualViewing is not null)
        {
            appointment.VirtualViewing.Status = VirtualViewingStatus.Cancelled;
        }

        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        var tenant = await _userReader.GetUserByIdAsync(appointment.TenantId, cancellationToken);
        var landlord = listing is not null
            ? await _userReader.GetUserByIdAsync(listing.LandlordId, cancellationToken)
            : null;

        var notifyTargetUserId = cancelledBy == ViewingCancelledBy.Tenant
            ? (listing?.LandlordId ?? 0)
            : appointment.TenantId;

        if (notifyTargetUserId > 0)
        {
            await _notificationPublisher.PublishAsync(
                userId: notifyTargetUserId,
                title: "Lịch xem phòng đã bị hủy",
                content: $"Lịch hẹn xem phòng '{listing?.Title ?? $"#{appointment.ListingId}"}' đã bị hủy bởi {(cancelledBy == ViewingCancelledBy.Tenant ? "người thuê" : "chủ nhà")}.",
                type: "VIEWING_CANCELLED",
                referenceType: "VIEWING_APPOINTMENT",
                referenceId: appointment.Id,
                cancellationToken: cancellationToken);
        }

        _logger.LogInformation(
            "Lịch xem phòng #{AppointmentId} đã bị hủy bởi {CancelledBy} (UserId: {ActorUserId})",
            appointment.Id,
            cancelledBy,
            command.ActorUserId);

        return Result.Success(ViewingAppointmentMapper.ToDto(appointment, listing, tenant, landlord));
    }
}
