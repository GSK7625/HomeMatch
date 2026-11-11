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

namespace HomeMatch.Modules.Viewings.Application.Commands.BookViewingAppointment;

public sealed class BookViewingAppointmentHandler
{
    private readonly IViewingAppointmentRepository _appointmentRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly ILogger<BookViewingAppointmentHandler> _logger;

    public BookViewingAppointmentHandler(
        IViewingAppointmentRepository appointmentRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader,
        INotificationPublisher notificationPublisher,
        ILogger<BookViewingAppointmentHandler> logger)
    {
        _appointmentRepository = appointmentRepository;
        _listingReader = listingReader;
        _userReader = userReader;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task<Result<ViewingAppointmentDto>> HandleAsync(
        BookViewingAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DurationMinutes < 15 || command.DurationMinutes > 180)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.InvalidDuration);
        }

        var now = DateTimeOffset.UtcNow;
        var appointmentTimeUtc = command.AppointmentTime.ToUniversalTime();
        if (appointmentTimeUtc <= now.AddMinutes(5))
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.TimeInPast);
        }

        var user = await _userReader.GetUserByIdAsync(command.TenantId, cancellationToken);
        if (user is not null && string.Equals(user.Role, "Landlord", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.LandlordBookingNotAllowed);
        }

        var listing = await _listingReader.GetListingByIdAsync(command.ListingId, cancellationToken);
        if (listing is null || !listing.IsPublished)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.ListingNotFound);
        }

        if (listing.LandlordId == command.TenantId)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.SelfBookingNotAllowed);
        }

        var relatedListingIds = await _listingReader.GetListingIdsByRoomIdAsync(listing.RoomId, cancellationToken);
        var checkListingIds = relatedListingIds.Count > 0 ? relatedListingIds : [command.ListingId];

        var hasOverlap = await _appointmentRepository.HasOverlappingAppointmentAsync(
            checkListingIds,
            appointmentTimeUtc,
            command.DurationMinutes,
            excludeAppointmentId: null,
            cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.ScheduleConflict);
        }

        var appointment = new ViewingAppointment
        {
            TenantId = command.TenantId,
            ListingId = command.ListingId,
            AppointmentTime = appointmentTimeUtc,
            DurationMinutes = command.DurationMinutes,
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            Status = ViewingAppointmentStatus.Pending,
            RemindAt = appointmentTimeUtc.AddHours(-2),
            IsReminded = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (command.IsVirtualViewing)
        {
            appointment.VirtualViewing = new VirtualViewing
            {
                RoomUrl = $"https://meet.homematch.vn/room-{command.ListingId}-{Guid.NewGuid():N}",
                Status = VirtualViewingStatus.Scheduled,
                CreatedAt = now
            };
        }

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        var tenant = await _userReader.GetUserByIdAsync(command.TenantId, cancellationToken);
        var landlord = await _userReader.GetUserByIdAsync(listing.LandlordId, cancellationToken);

        await _notificationPublisher.PublishAsync(
            userId: listing.LandlordId,
            title: "Yêu cầu đặt lịch xem phòng mới",
            content: $"{tenant?.FullName ?? "Người thuê"} đã đặt lịch xem phòng '{listing.Title}' vào lúc {appointmentTimeUtc:dd/MM/yyyy HH:mm} UTC.",
            type: "VIEWING_BOOKED",
            referenceType: "VIEWING_APPOINTMENT",
            referenceId: appointment.Id,
            cancellationToken: cancellationToken);

        await _notificationPublisher.PublishAsync(
            userId: command.TenantId,
            title: "Đã gửi yêu cầu đặt lịch xem phòng",
            content: $"Lịch hẹn xem phòng '{listing.Title}' vào lúc {appointmentTimeUtc:dd/MM/yyyy HH:mm} UTC đang chờ chủ nhà xác nhận.",
            type: "VIEWING_BOOKED",
            referenceType: "VIEWING_APPOINTMENT",
            referenceId: appointment.Id,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Đã tạo lịch xem phòng #{AppointmentId} cho người thuê {TenantId} tại tin đăng {ListingId} vào lúc {AppointmentTime}",
            appointment.Id,
            appointment.TenantId,
            appointment.ListingId,
            appointment.AppointmentTime);

        return Result.Success(ViewingAppointmentMapper.ToDto(appointment, listing, tenant, landlord));
    }
}
