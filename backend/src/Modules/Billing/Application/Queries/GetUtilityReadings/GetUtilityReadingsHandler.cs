using HomeMatch.Modules.Billing.Application.Commands.RecordUtilityReading;
using HomeMatch.Modules.Billing.Application.DTOs;
using HomeMatch.Modules.Billing.Application.Interfaces;
using HomeMatch.Modules.Billing.Domain.Constants;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Application.Queries.GetUtilityReadings;

public class GetUtilityReadingsHandler
{
    private readonly IUtilityReadingRepository _readingRepository;

    public GetUtilityReadingsHandler(IUtilityReadingRepository readingRepository)
    {
        _readingRepository = readingRepository;
    }

    public async Task<Result<IReadOnlyList<UtilityReadingDto>>> HandleAsync(
        GetUtilityReadingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!query.IsAdmin && !query.IsLandlord && !query.IsTenant)
        {
            return Result.Failure<IReadOnlyList<UtilityReadingDto>>(BillingErrors.Forbidden);
        }

        var scopedFilter = query.Filter;
        if (!query.IsAdmin)
        {
            if (query.IsLandlord)
            {
                scopedFilter = scopedFilter with { LandlordId = query.RequestUserId };
            }
            else if (query.IsTenant)
            {
                scopedFilter = scopedFilter with { TenantId = query.RequestUserId };
            }
        }

        var items = await _readingRepository.GetListAsync(scopedFilter, cancellationToken);
        var dtos = items.Select(RecordUtilityReadingHandler.ToDto).ToList();
        return Result.Success<IReadOnlyList<UtilityReadingDto>>(dtos);
    }
}
