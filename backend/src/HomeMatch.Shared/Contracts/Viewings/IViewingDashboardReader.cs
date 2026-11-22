namespace HomeMatch.Shared.Contracts.Viewings;

public sealed record DashboardViewingInfo(
    long AppointmentId,
    long ListingId,
    long TenantId,
    DateTimeOffset AppointmentTime,
    bool IsVirtual,
    string Status);

public interface IViewingDashboardReader
{
    Task<IReadOnlyList<DashboardViewingInfo>> GetViewingsByListingIdsAsync(
        IReadOnlyCollection<long> listingIds,
        CancellationToken cancellationToken = default);
}
