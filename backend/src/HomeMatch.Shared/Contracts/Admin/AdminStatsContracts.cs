namespace HomeMatch.Shared.Contracts.Admin;

public sealed record AdminIdentityStats(
    int TotalUsers,
    int TotalTenants,
    int TotalLandlords,
    int SuspendedUsers,
    int NewUsersInPeriod);

public sealed record AdminPropertyStats(
    int TotalProperties,
    int TotalRooms,
    int AvailableRooms,
    int RentedRooms,
    int SuspendedRooms,
    int NewRoomsInPeriod);

public interface IAdminIdentityStatsReader
{
    Task<AdminIdentityStats> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken = default);
}

public interface IAdminPropertyStatsReader
{
    Task<AdminPropertyStats> GetStatsAsync(DateTimeOffset? fromUtc, CancellationToken cancellationToken = default);
}
