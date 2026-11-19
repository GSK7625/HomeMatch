using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Listings.Application.Interfaces;

public sealed record PriceEstimateRequest(
    string Province,
    string District,
    string? Ward,
    double? Lat,
    double? Lng,
    decimal AreaSqm,
    string RoomType,
    string PropertyType,
    int? Floor,
    int? MaxOccupants,
    IReadOnlyList<string> AmenityCodes);

public sealed record PriceFactorDto(string Label, long ImpactVnd);

public sealed record PriceEstimateResult(
    long P10,
    long P50,
    long P90,
    string Currency,
    string Unit,
    IReadOnlyList<PriceFactorDto> Factors,
    IReadOnlyList<string> Warnings,
    string ModelVersion);

public interface IPriceEstimationService
{
    Task<Result<PriceEstimateResult>> EstimateAsync(PriceEstimateRequest request, CancellationToken cancellationToken = default);
}
