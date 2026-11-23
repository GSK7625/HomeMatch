using System.Text.Json;
using HomeMatch.Modules.Properties.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeMatch.Modules.Properties.Infrastructure.Persistence.Seed;

public static class PropertiesDataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedAsync(PropertiesDbContext db, ILogger? logger = null)
    {
        await SeedProvincesAsync(db, logger);
        await SeedWardsAsync(db, logger);
        await SeedAmenitiesAsync(db, logger);
    }

    private static async Task SeedProvincesAsync(PropertiesDbContext db, ILogger? logger)
    {
        var existingCodes = await db.AdministrativeProvinces.Select(p => p.Code).ToHashSetAsync();
        if (existingCodes.Count >= 34)
        {
            return;
        }

        var provinces = LoadDataFromJson<AdministrativeProvince>("vietnam_provinces.json");
        if (provinces is null || provinces.Count == 0)
        {
            return;
        }

        var toAdd = provinces.Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (toAdd.Count > 0)
        {
            await db.AdministrativeProvinces.AddRangeAsync(toAdd);
            await db.SaveChangesAsync();
            logger?.LogInformation("Đã nạp toàn bộ {Count} tỉnh thành chính thức của Việt Nam (QĐ 19/2025/QĐ-TTg).", toAdd.Count);
        }
    }

    private static async Task SeedWardsAsync(PropertiesDbContext db, ILogger? logger)
    {
        var existingWardCount = await db.AdministrativeWards.CountAsync();
        if (existingWardCount >= 3321)
        {
            return;
        }

        var existingWardCodes = await db.AdministrativeWards.Select(w => w.Code).ToHashSetAsync();
        var wards = LoadDataFromJson<AdministrativeWard>("vietnam_wards.json");
        if (wards is null || wards.Count == 0)
        {
            return;
        }

        var toAdd = wards.Where(w => !existingWardCodes.Contains(w.Code)).ToList();
        if (toAdd.Count > 0)
        {
            const int batchSize = 1000;
            for (var i = 0; i < toAdd.Count; i += batchSize)
            {
                var batch = toAdd.Skip(i).Take(batchSize).ToList();
                await db.AdministrativeWards.AddRangeAsync(batch);
                await db.SaveChangesAsync();
            }

            logger?.LogInformation("Đã nạp thành công toàn bộ {Count} xã/phường/đặc khu chính thức toàn quốc (QĐ 19/2025/QĐ-TTg).", toAdd.Count);
        }
    }

    private static async Task SeedAmenitiesAsync(PropertiesDbContext db, ILogger? logger)
    {
        var officialAmenities = new List<Amenity>
        {
            new() { Name = "điều hòa", Code = "AIR_CONDITIONER" },
            new() { Name = "bếp", Code = "KITCHEN" },
            new() { Name = "tủ quần áo", Code = "WARDROBE" },
            new() { Name = "ban công", Code = "BALCONY" },
            new() { Name = "an ninh 24/7", Code = "SECURITY_24_7" },
            new() { Name = "giường", Code = "BED" },
            new() { Name = "chỗ để xe", Code = "PARKING" },
            new() { Name = "tủ lạnh", Code = "REFRIGERATOR" },
            new() { Name = "thang máy", Code = "ELEVATOR" },
            new() { Name = "máy giặt", Code = "WASHING_MACHINE" },
            new() { Name = "wifi", Code = "WIFI" },
            new() { Name = "camera", Code = "CAMERA" },
            new() { Name = "hồ bơi", Code = "SWIMMING_POOL" },
            new() { Name = "cửa sổ", Code = "WINDOW" },
            new() { Name = "phòng gym", Code = "GYM" },
            new() { Name = "nóng lạnh", Code = "WATER_HEATER" },
            new() { Name = "nội thất đầy đủ", Code = "FULLY_FURNISHED" },
            new() { Name = "gác lửng", Code = "MEZZANINE" },
            new() { Name = "khép kín", Code = "PRIVATE_BATHROOM" }
        };

        var validCodes = officialAmenities.Select(a => a.Code).ToHashSet();
        var existing = await db.Amenities.ToListAsync();
        
        // Xóa những tiện ích không nằm trong danh mục 19 tiện ích chuẩn nếu chưa có phòng nào dùng
        var obsolete = existing.Where(a => !validCodes.Contains(a.Code)).ToList();
        if (obsolete.Count > 0)
        {
            var obsoleteIds = obsolete.Select(a => a.Id).ToList();
            var usedIds = await db.RoomAmenities.Where(ra => obsoleteIds.Contains(ra.AmenityId)).Select(ra => ra.AmenityId).ToHashSetAsync();
            var canDelete = obsolete.Where(a => !usedIds.Contains(a.Id)).ToList();
            if (canDelete.Count > 0)
            {
                db.Amenities.RemoveRange(canDelete);
                await db.SaveChangesAsync();
                existing = await db.Amenities.ToListAsync();
            }
        }

        var existingByCode = existing.ToDictionary(a => a.Code);
        var added = 0;
        var updated = 0;

        foreach (var item in officialAmenities)
        {
            if (existingByCode.TryGetValue(item.Code, out var current))
            {
                if (current.Name != item.Name)
                {
                    current.Name = item.Name;
                    updated++;
                }
            }
            else
            {
                db.Amenities.Add(item);
                added++;
            }
        }

        if (added > 0 || updated > 0)
        {
            await db.SaveChangesAsync();
            logger?.LogInformation("Đã đồng bộ tiện ích: thêm mới {Added}, cập nhật {Updated}", added, updated);
        }
    }

    private static List<T>? LoadDataFromJson<T>(string fileName)
    {
        var filePath = FindDataFilePath(fileName);
        if (filePath is null || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindDataFilePath(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Persistence", "Seed", "Data", fileName),
            Path.Combine(AppContext.BaseDirectory, "Seed", "Data", fileName),
            Path.Combine(AppContext.BaseDirectory, "Data", fileName),
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Persistence", "Seed", "Data", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Modules", "Properties", "Infrastructure", "Persistence", "Seed", "Data", fileName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
