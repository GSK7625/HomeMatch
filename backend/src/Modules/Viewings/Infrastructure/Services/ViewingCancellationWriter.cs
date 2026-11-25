using HomeMatch.Modules.Viewings.Domain.Enums;
using HomeMatch.Modules.Viewings.Infrastructure.Persistence;
using HomeMatch.Shared.Contracts.Viewings;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Viewings.Infrastructure.Services;

public sealed class ViewingCancellationWriter(ViewingsDbContext db) : IViewingCancellationWriter
{
    public async Task CancelForListingAsync(long listingId, CancellationToken cancellationToken)
    {
        const string reason = "Tin đăng đã xóa";
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.ViewingAppointments
            .Where(a => a.ListingId == listingId
                && (a.Status == ViewingAppointmentStatus.Pending || a.Status == ViewingAppointmentStatus.Confirmed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Status, ViewingAppointmentStatus.Cancelled)
                .SetProperty(a => a.CancelledBy, ViewingCancelledBy.System)
                .SetProperty(a => a.CancelledReason, reason)
                .SetProperty(a => a.UpdatedAt, now), cancellationToken);
        await db.VirtualViewings
            .Where(v => v.Appointment != null && v.Appointment.ListingId == listingId
                && v.Appointment.Status == ViewingAppointmentStatus.Cancelled
                && v.Appointment.CancelledBy == ViewingCancelledBy.System && v.Appointment.CancelledReason == reason
                && v.Status != VirtualViewingStatus.Cancelled)
            .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.Status, VirtualViewingStatus.Cancelled), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
