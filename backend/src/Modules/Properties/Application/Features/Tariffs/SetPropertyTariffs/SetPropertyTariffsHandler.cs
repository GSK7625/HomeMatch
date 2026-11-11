using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Properties.Application.Features.Tariffs.SetPropertyTariffs;

public sealed class SetPropertyTariffsHandler
{
    private readonly IPropertyRepository _properties;
    private readonly ITariffRepository _tariffs;

    public SetPropertyTariffsHandler(IPropertyRepository properties, ITariffRepository tariffs)
    {
        _properties = properties;
        _tariffs = tariffs;
    }

    public async Task<Result<IReadOnlyList<TariffResult>>> HandleAsync(
        SetPropertyTariffsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await _properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure<IReadOnlyList<TariffResult>>(PropertiesErrors.PropertyNotFound);

        if (command.Tariffs is null || command.Tariffs.Count == 0)
            return Result.Failure<IReadOnlyList<TariffResult>>(new Error("Request.Invalid", "Danh sách biểu giá không được để trống."));

        var distinctCount = command.Tariffs.Select(t => t.TariffType).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinctCount != command.Tariffs.Count)
            return Result.Failure<IReadOnlyList<TariffResult>>(PropertiesErrors.DuplicateTariffType);

        var tuples = new List<(string TariffType, string CalcMethod, long UnitPrice)>(command.Tariffs.Count);
        foreach (var item in command.Tariffs)
        {
            var error = TariffValidation.Validate(item.TariffType, item.CalcMethod, item.UnitPrice);
            if (error is not null)
                return Result.Failure<IReadOnlyList<TariffResult>>(error);

            if (item.CalcMethod == "FREE" && item.UnitPrice != 0)
                return Result.Failure<IReadOnlyList<TariffResult>>(PropertiesErrors.InvalidTariff);

            tuples.Add((item.TariffType, item.CalcMethod, item.UnitPrice));
        }

        var saved = await _tariffs.SetPropertyTariffsAsync(command.PropertyId, tuples, cancellationToken);
        if (saved.IsFailure)
            return Result.Failure<IReadOnlyList<TariffResult>>(saved.Error);

        return Result.Success<IReadOnlyList<TariffResult>>(saved.Value.Select(PropertyMapping.ToResult).ToArray());
    }
}

