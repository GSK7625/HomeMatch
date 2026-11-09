using HomeMatch.Modules.Viewings.Application.DTOs;
using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Modules.Viewings.Domain.Constants;
using HomeMatch.Modules.Viewings.Domain.Enums;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Viewings.Application.Commands.CompleteViewingAppointment;

public sealed class CompleteViewingAppointmentHandler
{
    private readonly IViewingAppointmentRepository _appointmentRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;
    private readonly ILogger<CompleteViewingAppointmentHandler> _logger;

    public CompleteViewingAppointmentHandler(
        IViewingAppointmentRepository appointmentRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader,
        ILogger<CompleteViewingAppointmentHandler> logger)
    {
        _appointmentRepository = appointmentRepository;
        _listingReader = listingReader;
        _userReader = userReader;
        _logger = logger;
    }

    public async Task<Result<ViewingAppointmentDto>> HandleAsync(
        CompleteViewingAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.NotFound);
        }

        var listing = await _listingReader.GetListingByIdAsync(appointment.ListingId, cancellationToken);

        var isTenant = appointment.TenantId == command.ActorUserId;
        var isLandlord = listing is not null && listing.LandlordId == command.ActorUserId;

        if (!isTenant && !isLandlord)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.Forbidden);
        }

        if (appointment.Status != ViewingAppointmentStatus.Confirmed)
        {
            return Result.Failure<ViewingAppointmentDto>(
                ViewingErrors.InvalidStatusTransition(appointment.Status, ViewingAppointmentStatus.Completed));
        }

        var now = DateTimeOffset.UtcNow;
        appointment.Status = ViewingAppointmentStatus.Completed;
        appointment.UpdatedAt = now;

        if (appointment.VirtualViewing is not null)
        {
            appointment.VirtualViewing.Status = VirtualViewingStatus.Ended;
            appointment.VirtualViewing.EndedAt ??= now;
        }

        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Buổi xem phòng #{AppointmentId} đã được đánh dấu hoàn thành bởi UserId {ActorUserId}",
            appointment.Id,
            command.ActorUserId);

        var tenant = await _userReader.GetUserByIdAsync(appointment.TenantId, cancellationToken);
        var landlord = listing is not null
            ? await _userReader.GetUserByIdAsync(listing.LandlordId, cancellationToken)
            : null;

        return Result.Success(ViewingAppointmentMapper.ToDto(appointment, listing, tenant, landlord));
    }
}
