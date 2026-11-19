using System.Text.Json;
using System.Text.Json.Nodes;
using HomeMatch.Modules.Leasing.Domain.Entities;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Identity;

namespace HomeMatch.Modules.Leasing.Application.DTOs;

/// <summary>
/// Tự động bổ sung thông tin thật của Chủ nhà, Người thuê, Phòng, Địa chỉ, Tiện nghi và Biểu phí
/// vào document_snapshot JSON để hợp đồng điện tử luôn hiển thị đầy đủ dữ liệu chính xác.
/// </summary>
public static class ContractSnapshotEnricher
{
    public static async Task<string> BuildEnrichedSnapshotAsync(
        string? existingSnapshot,
        long landlordId,
        long tenantId,
        long roomId,
        IIdentityUserReader userReader,
        IRoomListingReader roomReader,
        CancellationToken cancellationToken)
    {
        var root = ParseOrCreateObject(existingSnapshot);

        var landlord = await userReader.GetUserByIdAsync(landlordId, cancellationToken);
        var tenant = await userReader.GetUserByIdAsync(tenantId, cancellationToken);
        var roomDetail = await roomReader.GetRoomDetailAsync(roomId, cancellationToken);

        // 1. Thông tin phòng, tòa nhà, địa chỉ đầy đủ, tiện nghi & biểu phí thật
        if (roomDetail is not null)
        {
            SetIfMissing(root, "roomName", $"Phòng {roomDetail.RoomNumber}");
            SetIfMissing(root, "propertyName", roomDetail.Property.Name);
            SetIfMissing(root, "roomType", roomDetail.RoomType);

            if (root["floor"] is null) root["floor"] = roomDetail.Floor;
            if (root["areaSqm"] is null) root["areaSqm"] = roomDetail.AreaSqm;
            if (root["maxOccupants"] is null) root["maxOccupants"] = roomDetail.MaxOccupants;

            var addressParts = new[]
            {
                roomDetail.Property.AddressDetail,
                roomDetail.Property.WardName,
                roomDetail.Property.DistrictName,
                roomDetail.Property.ProvinceName
            }.Where(x => !string.IsNullOrWhiteSpace(x));
            var fullAddress = string.Join(", ", addressParts);
            if (!string.IsNullOrWhiteSpace(fullAddress))
            {
                SetIfMissing(root, "propertyAddress", fullAddress);
            }

            if (root["amenities"] is null && roomDetail.Amenities.Count > 0)
            {
                var arr = new JsonArray();
                foreach (var a in roomDetail.Amenities)
                {
                    arr.Add(a.Name);
                }
                root["amenities"] = arr;
            }

            if (roomDetail.Tariffs.Count > 0)
            {
                foreach (var t in roomDetail.Tariffs)
                {
                    var formatted = FormatTariff(t);
                    var key = t.TariffType.ToUpperInvariant() switch
                    {
                        "ELECTRICITY" => "electricityPrice",
                        "WATER" => "waterPrice",
                        "INTERNET" => "internetPrice",
                        "SERVICE" or "CLEANING" => "serviceFee",
                        "PARKING" => "parkingFee",
                        _ => null
                    };
                    if (key is not null)
                    {
                        SetIfMissing(root, key, formatted);
                    }
                }
            }
        }

        // 2. Thông tin Bên A (Chủ nhà)
        var landlordNode = GetOrCreateChild(root, "landlord");
        if (landlord is not null)
        {
            SetIfMissing(landlordNode, "fullName", CleanDisplayName(landlord.FullName));
            SetIfMissing(landlordNode, "email", landlord.Email);
            if (!string.IsNullOrWhiteSpace(landlord.PhoneNumber))
            {
                SetIfMissing(landlordNode, "phone", landlord.PhoneNumber);
            }
        }

        // 3. Thông tin Bên B (Người thuê)
        var tenantNode = GetOrCreateChild(root, "tenant");
        if (tenant is not null)
        {
            SetIfMissing(tenantNode, "fullName", CleanDisplayName(tenant.FullName));
            SetIfMissing(tenantNode, "email", tenant.Email);
            if (!string.IsNullOrWhiteSpace(tenant.PhoneNumber))
            {
                SetIfMissing(tenantNode, "phone", tenant.PhoneNumber);
            }
        }

        return root.ToJsonString();
    }

    private static JsonObject ParseOrCreateObject(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new JsonObject();
        try
        {
            if (JsonNode.Parse(raw) is JsonObject obj) return obj;
        }
        catch
        {
            return new JsonObject { ["terms"] = raw.Trim() };
        }
        return new JsonObject();
    }

    private static JsonObject GetOrCreateChild(JsonObject parent, string key)
    {
        if (parent[key] is JsonObject child) return child;
        var created = new JsonObject();
        parent[key] = created;
        return created;
    }

    private static void SetIfMissing(JsonObject node, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var current = node[key]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(current))
        {
            node[key] = value.Trim();
        }
    }

    private static string CleanDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var idx = name.IndexOf('(');
        return (idx > 0 ? name[..idx] : name).Trim();
    }

    private static string FormatTariff(RoomListingTariffSnapshot tariff)
    {
        if (string.Equals(tariff.CalcMethod, "FREE", StringComparison.OrdinalIgnoreCase) || tariff.UnitPrice == 0)
        {
            return "Miễn phí";
        }

        var unit = tariff.CalcMethod.ToUpperInvariant() switch
        {
            "PER_KWH" => "đ/kWh",
            "PER_M3" => "đ/m³",
            "PER_PERSON" => "đ/người/tháng",
            "FIXED_PER_MONTH" => "đ/phòng/tháng",
            _ => "đ"
        };

        return $"{tariff.UnitPrice:N0} {unit}".Replace(",", ".");
    }
}
