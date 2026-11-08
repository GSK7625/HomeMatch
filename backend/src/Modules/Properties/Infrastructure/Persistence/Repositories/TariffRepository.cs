using HomeMatch.Modules.Properties.Application;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Repositories;

public sealed class TariffRepository(PropertiesDbContext db) : ITariffRepository
{
    public async Task<IReadOnlyList<UtilityTariff>> GetPropertyTariffsAsync(long propertyId, CancellationToken cancellationToken) =>
        await db.UtilityTariffs
            .AsNoTracking()
            .Where(t => t.PropertyId == propertyId && t.RoomId == null)
            .OrderBy(t => t.TariffType)
            .ToListAsync(cancellationToken);

    public async Task<Result<IReadOnlyList<UtilityTariff>>> SetPropertyTariffsAsync(
        long propertyId,
        IReadOnlyList<(string TariffType, string CalcMethod, long UnitPrice)> tariffs,
        CancellationToken cancellationToken)
    {
        var existing = await db.UtilityTariffs
            .Where(t => t.PropertyId == propertyId && t.RoomId == null)
            .ToListAsync(cancellationToken);

        var existingByType = existing.ToDictionary(t => t.TariffType, StringComparer.OrdinalIgnoreCase);
        var targetTypes = new HashSet<string>(tariffs.Select(t => t.TariffType), StringComparer.OrdinalIgnoreCase);

        // Xóa những loại phí không còn trong danh sách mới
        foreach (var old in existing)
        {
            if (!targetTypes.Contains(old.TariffType))
            {
                db.UtilityTariffs.Remove(old);
            }
        }

        var today = BusinessDate.Today();
        var resultList = new List<UtilityTariff>();

        // Cập nhật hoặc thêm mới
        foreach (var item in tariffs)
        {
            if (existingByType.TryGetValue(item.TariffType, out var existingTariff))
            {
                existingTariff.CalcMethod = item.CalcMethod;
                existingTariff.UnitPrice = item.UnitPrice;
                existingTariff.EffectiveFrom = today;
                resultList.Add(existingTariff);
            }
            else
            {
                var created = new UtilityTariff
                {
                    PropertyId = propertyId,
                    RoomId = null,
                    TariffType = item.TariffType,
                    CalcMethod = item.CalcMethod,
                    UnitPrice = item.UnitPrice,
                    EffectiveFrom = today,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                db.UtilityTariffs.Add(created);
                resultList.Add(created);
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success<IReadOnlyList<UtilityTariff>>(resultList.OrderBy(t => t.TariffType).ToList());
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Result.Failure<IReadOnlyList<UtilityTariff>>(PropertiesErrors.DuplicateTariffType);
        }
    }
}
