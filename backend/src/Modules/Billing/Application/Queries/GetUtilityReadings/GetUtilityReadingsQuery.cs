using HomeMatch.Modules.Billing.Application.DTOs;

namespace HomeMatch.Modules.Billing.Application.Queries.GetUtilityReadings;

public sealed record GetUtilityReadingsQuery(
    UtilityReadingFilter Filter,
    long RequestUserId,
    bool IsLandlord,
    bool IsTenant,
    bool IsAdmin);
