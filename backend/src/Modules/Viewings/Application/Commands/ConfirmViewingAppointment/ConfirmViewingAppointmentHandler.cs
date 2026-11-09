using HomeMatch.Modules.Viewings.Application.DTOs;
using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Modules.Viewings.Domain.Constants;
using HomeMatch.Modules.Viewings.Domain.Entities;
using HomeMatch.Modules.Viewings.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Viewings.Application.Commands.ConfirmViewingAppointment;

public sealed class ConfirmViewingAppointmentHandler
{
    private readonly IViewingAppointmentRepository _appointmentRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<ConfirmViewingAppointmentHandler> _logger;

    public ConfirmViewingAppointmentHandler(
        IViewingAppointmentRepository appointmentRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader,
        INotificationPublisher notificationPublisher,
        ILogger<ConfirmViewingAppointmentHandler> logger)
    {
        _appointmentRepository = appointmentRepository;
        _listingReader = listingReader;
        _userReader = userReader;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result<ViewingAppointmentDto>> HandleAsync(
        ConfirmViewingAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.NotFound);
        }

        var listing = await _listingReader.GetListingByIdAsync(appointment.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.ListingNotFound);
        }

        if (listing.LandlordId != command.ActorUserId)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.Forbidden);
        }

        if (appointment.Status != ViewingAppointmentStatus.Pending)
        {
            return Result.Failure<ViewingAppointmentDto>(
                ViewingErrors.InvalidStatusTransition(appointment.Status, ViewingAppointmentStatus.Confirmed));
        }

        var relatedListingIds = await _listingReader.GetListingIdsByRoomIdAsync(listing.RoomId, cancellationToken);
        var checkListingIds = relatedListingIds.Count > 0 ? relatedListingIds : [appointment.ListingId];

        var hasOverlap = await _appointmentRepository.HasOverlappingAppointmentAsync(
            checkListingIds,
            appointment.AppointmentTime,
            appointment.DurationMinutes,
            excludeAppointmentId: appointment.Id,
            cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.ScheduleConflict);
        }

        appointment.Status = ViewingAppointmentStatus.Confirmed;
        appointment.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(command.VirtualRoomUrl))
        {
            if (appointment.VirtualViewing is null)
            {
                appointment.VirtualViewing = new VirtualViewing
                {
                    AppointmentId = appointment.Id,
                    RoomUrl = command.VirtualRoomUrl.Trim(),
                    Status = VirtualViewingStatus.Scheduled,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }
            else
            {
                appointment.VirtualViewing.RoomUrl = command.VirtualRoomUrl.Trim();
            }
        }

        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        var tenant = await _userReader.GetUserByIdAsync(appointment.TenantId, cancellationToken);
        var landlord = await _userReader.GetUserByIdAsync(listing.LandlordId, cancellationToken);

        await _notificationPublisher.PublishAsync(
            userId: appointment.TenantId,
            title: "Lịch xem phòng đã được xác nhận",
            content: $"Chủ nhà {landlord?.FullName ?? ""} đã xác nhận lịch xem phòng '{listing.Title}' vào lúc {appointment.AppointmentTime:dd/MM/yyyy HH:mm} UTC.",
            type: "VIEWING_CONFIRMED",
            referenceType: "VIEWING_APPOINTMENT",
            referenceId: appointment.Id,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Chủ nhà {LandlordId} đã xác nhận lịch xem phòng #{AppointmentId}",
            command.ActorUserId,
            appointment.Id);

        return Result.Success(ViewingAppointmentMapper.ToDto(appointment, listing, tenant, landlord));
    }
}
