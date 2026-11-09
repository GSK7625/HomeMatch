using HomeMatch.Modules.Viewings.Application.DTOs;
using HomeMatch.Modules.Viewings.Application.Interfaces;
using HomeMatch.Modules.Viewings.Domain.Constants;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts.Identity;
using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Viewings.Application.Queries.GetViewingAppointmentById;

public sealed class GetViewingAppointmentByIdHandler
{
    private readonly IViewingAppointmentRepository _appointmentRepository;
    private readonly IListingContractReader _listingReader;
    private readonly IIdentityUserReader _userReader;

    public GetViewingAppointmentByIdHandler(
        IViewingAppointmentRepository appointmentRepository,
        IListingContractReader listingReader,
        IIdentityUserReader userReader)
    {
        _appointmentRepository = appointmentRepository;
        _listingReader = listingReader;
        _userReader = userReader;
    }

    public async Task<Result<ViewingAppointmentDto>> HandleAsync(
        GetViewingAppointmentByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(query.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.NotFound);
        }

        var listing = await _listingReader.GetListingByIdAsync(appointment.ListingId, cancellationToken);

        var isTenant = appointment.TenantId == query.ActorUserId;
        var isLandlord = listing is not null && listing.LandlordId == query.ActorUserId;

        if (!isTenant && !isLandlord)
        {
            return Result.Failure<ViewingAppointmentDto>(ViewingErrors.Forbidden);
        }

        var tenant = await _userReader.GetUserByIdAsync(appointment.TenantId, cancellationToken);
        var landlord = listing is not null
            ? await _userReader.GetUserByIdAsync(listing.LandlordId, cancellationToken)
            : null;

        return Result.Success(ViewingAppointmentMapper.ToDto(appointment, listing, tenant, landlord));
    }
}
