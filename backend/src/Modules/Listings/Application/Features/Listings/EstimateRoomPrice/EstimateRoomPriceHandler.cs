using HomeMatch.Modules.Listings.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;

namespace HomeMatch.Modules.Listings.Application.Features.Listings.EstimateRoomPrice;

public sealed record EstimateRoomPriceQuery(long RoomId);

public sealed class EstimateRoomPriceHandler(
    IRoomListingReader rooms,
    IPriceEstimationService priceService)
{
    public async Task<Result<PriceEstimateResult>> HandleAsync(
        EstimateRoomPriceQuery query,
        CancellationToken cancellationToken = default)
    {
        var detail = await rooms.GetRoomDetailAsync(query.RoomId, cancellationToken);
        if (detail is null)
        {
            return Result.Failure<PriceEstimateResult>(new Error("Listings.RoomNotFound", "Không tìm thấy thông tin phòng."));
        }

        var snapshot = await rooms.GetRoomAsync(query.RoomId, cancellationToken);

        var mappedRoomType = detail.RoomType.ToUpperInvariant() switch
        {
            "SINGLE" => "ROOM",
            _ => detail.RoomType.ToUpperInvariant()
        };

        var amenityCodes = detail.Amenities
            .Select(a => MapAmenityCode(a.Code, a.Name))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToArray();

        var request = new PriceEstimateRequest(
            Province: detail.Property.ProvinceName,
            District: detail.Property.DistrictName ?? detail.Property.WardName,
            Ward: detail.Property.WardName,
            Lat: snapshot?.Latitude is { } lat ? (double)lat : null,
            Lng: snapshot?.Longitude is { } lng ? (double)lng : null,
            AreaSqm: detail.AreaSqm,
            RoomType: mappedRoomType,
            PropertyType: detail.Property.PropertyType,
            Floor: detail.Floor,
            MaxOccupants: detail.MaxOccupants,
            AmenityCodes: amenityCodes);

        return await priceService.EstimateAsync(request, cancellationToken);
    }

    private static string MapAmenityCode(string? code, string? name)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        if (c is "AIR_CONDITIONER" or "WATER_HEATER" or "WASHING_MACHINE" or "SMART_LOCK"
            or "PRIVATE_WC" or "BED" or "WARDROBE" or "KITCHEN" or "BALCONY" or "ELEVATOR"
            or "PARKING" or "LOFT" or "WINDOW" or "WIFI" or "FRIDGE" or "FURNISHED_FULL")
        {
            return c;
        }

        var n = (name ?? "").Trim().ToLowerInvariant();
        if (n.Contains("điều hòa")) return "AIR_CONDITIONER";
        if (n.Contains("nóng lạnh")) return "WATER_HEATER";
        if (n.Contains("máy giặt")) return "WASHING_MACHINE";
        if (n.Contains("vân tay") || n.Contains("khóa")) return "SMART_LOCK";
        if (n.Contains("khép kín") || n.Contains("vệ sinh")) return "PRIVATE_WC";
        if (n.Contains("giường")) return "BED";
        if (n.Contains("tủ quần áo")) return "WARDROBE";
        if (n.Contains("bếp")) return "KITCHEN";
        if (n.Contains("ban công")) return "BALCONY";
        if (n.Contains("thang máy")) return "ELEVATOR";
        if (n.Contains("để xe")) return "PARKING";
        if (n.Contains("gác lửng")) return "LOFT";
        if (n.Contains("cửa sổ")) return "WINDOW";
        if (n.Contains("wifi")) return "WIFI";
        if (n.Contains("tủ lạnh")) return "FRIDGE";
        if (n.Contains("nội thất")) return "FURNISHED_FULL";
        return string.Empty;
    }
}
