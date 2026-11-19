using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMatch.Modules.Listings.Infrastructure.Services;

public sealed class HttpPriceEstimationService(
    HttpClient httpClient,
    IOptions<PriceEstimationOptions> options,
    ILogger<HttpPriceEstimationService> logger) : IPriceEstimationService
{
    private readonly PriceEstimationOptions _options = options.Value;

    public async Task<Result<PriceEstimateResult>> EstimateAsync(
        PriceEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                province = request.Province,
                district = request.District,
                ward = request.Ward,
                lat = request.Lat,
                lng = request.Lng,
                area_sqm = (double)request.AreaSqm,
                room_type = request.RoomType,
                property_type = request.PropertyType,
                floor = request.Floor,
                max_occupants = request.MaxOccupants,
                amenity_codes = request.AmenityCodes,
                explain = true
            };

            using var msg = new HttpRequestMessage(HttpMethod.Post, "/v1/price-estimate");
            if (!string.IsNullOrWhiteSpace(_options.InternalApiKey))
            {
                msg.Headers.TryAddWithoutValidation("X-Internal-Key", _options.InternalApiKey);
            }
            msg.Content = JsonContent.Create(payload);

            using var response = await httpClient.SendAsync(msg, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("PriceModel service returned {StatusCode}: {Body}", (int)response.StatusCode, errBody);
                return Result.Failure<PriceEstimateResult>(new Error(
                    "Listings.PriceEstimateFailed",
                    $"Dịch vụ gợi ý giá AI chưa hỗ trợ khu vực hoặc dữ liệu này ({(int)response.StatusCode})."));
            }

            var data = await response.Content.ReadFromJsonAsync<FastApiEstimateResponse>(cancellationToken: cancellationToken);
            if (data is null)
            {
                return Result.Failure<PriceEstimateResult>(new Error(
                    "Listings.PriceEstimateEmpty",
                    "Không nhận được phản hồi hợp lệ từ dịch vụ định giá AI."));
            }

            var factors = data.Factors?
                .Select(f => new PriceFactorDto(f.Label ?? string.Empty, f.ImpactVnd))
                .ToArray() ?? [];

            return Result.Success(new PriceEstimateResult(
                data.P10,
                data.P50,
                data.P90,
                data.Currency ?? "VND",
                data.Unit ?? "per_month",
                factors,
                data.Warnings ?? [],
                data.ModelVersion ?? "v1"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể kết nối tới PriceModel service tại {BaseUrl}", httpClient.BaseAddress);
            return Result.Failure<PriceEstimateResult>(new Error(
                "Listings.PriceServiceUnavailable",
                "Dịch vụ gợi ý giá AI đang tạm thời gián đoạn. Vui lòng thử lại sau."));
        }
    }

    private sealed class FastApiEstimateResponse
    {
        [JsonPropertyName("p10")] public long P10 { get; set; }
        [JsonPropertyName("p50")] public long P50 { get; set; }
        [JsonPropertyName("p90")] public long P90 { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
        [JsonPropertyName("factors")] public List<FastApiFactor>? Factors { get; set; }
        [JsonPropertyName("warnings")] public List<string>? Warnings { get; set; }
        [JsonPropertyName("model_version")] public string? ModelVersion { get; set; }
    }

    private sealed class FastApiFactor
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("impact_vnd")] public long ImpactVnd { get; set; }
    }
}
