using HomeMatch.Modules.Properties.Application.DTOs;
using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Modules.Properties.Domain.Entities;
using HomeMatch.Modules.Properties.Infrastructure.Persistence;
using HomeMatch.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeMatch.Modules.Properties.Application;

/// <summary>
/// Dịch vụ quản lý Đơn giá dịch vụ tiện ích (điện, nước, internet...).
/// Tuân thủ SRP — chỉ xử lý nghiệp vụ liên quan đến Đơn giá.
/// Tuân thủ DIP — implement ITariffService, được inject qua interface.
/// </summary>
public sealed class TariffService(PropertiesDbContext db) : ITariffService
{
    /// Thiết lập biểu giá điện/nước/dịch vụ mới
    public async Task<TariffResponse> CreateAsync(
        long landlordId, long propertyId,
        CreateTariffRequest request, CancellationToken cancellationToken)
    {
        await EnsurePropertyOwnershipAsync(landlordId, propertyId, cancellationToken);
        Validate(request.TariffType, request.CalcMethod, request.UnitPrice);
        await EnsureUniqueDateAsync(propertyId, request.TariffType,
            request.EffectiveFrom, null, cancellationToken);

        var tariff = new UtilityTariff
        {
            PropertyId = propertyId,
            TariffType = request.TariffType,
            CalcMethod = request.CalcMethod,
            UnitPrice = request.UnitPrice,
            EffectiveFrom = request.EffectiveFrom
        };
        db.UtilityTariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(tariff);
    }

    /// Cập nhật biểu giá dịch vụ (chỉ sửa khi giá chưa có hiệu lực)
    public async Task<TariffResponse> UpdateAsync(
        long landlordId, long propertyId, long tariffId,
        UpdateTariffRequest request, CancellationToken cancellationToken)
    {
        await EnsurePropertyOwnershipAsync(landlordId, propertyId, cancellationToken);

        var tariff = await db.UtilityTariffs.FirstOrDefaultAsync(
            t => t.Id == tariffId && t.PropertyId == propertyId && t.RoomId == null,
            cancellationToken)
            ?? throw ApiException.NotFound("Tariff.NotFound", "Không tìm thấy đơn giá.");

        if (tariff.EffectiveFrom <= BusinessDate.Today())
            throw ApiException.Conflict("Tariff.AlreadyEffective",
                "Đơn giá đã có hiệu lực. Hãy tạo mức giá mới với ngày hiệu lực mới.");

        var method = request.CalcMethod ?? tariff.CalcMethod;
        var price = request.UnitPrice ?? tariff.UnitPrice;
        var date = request.EffectiveFrom ?? tariff.EffectiveFrom;

        Validate(tariff.TariffType, method, price);

        if (date <= BusinessDate.Today())
            throw ApiException.BadRequest("Tariff.InvalidDate",
                "Chỉ có thể sửa đơn giá chưa có hiệu lực.");

        await EnsureUniqueDateAsync(propertyId, tariff.TariffType,
            date, tariffId, cancellationToken);

        tariff.CalcMethod = method;
        tariff.UnitPrice = price;
        tariff.EffectiveFrom = date;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(tariff);
    }

    /// Xem lịch sử các mức giá dịch vụ theo thời gian
    public async Task<IReadOnlyList<TariffResponse>> GetHistoryAsync(
        long landlordId, long propertyId, CancellationToken cancellationToken)
    {
        await EnsurePropertyOwnershipAsync(landlordId, propertyId, cancellationToken);
        return await db.UtilityTariffs.AsNoTracking()
            .Where(t => t.PropertyId == propertyId && t.RoomId == null)
            .OrderBy(t => t.TariffType).ThenByDescending(t => t.EffectiveFrom)
            .Select(t => new TariffResponse(t.Id, t.PropertyId, t.RoomId,
                t.TariffType, t.CalcMethod, t.UnitPrice, t.EffectiveFrom))
            .ToListAsync(cancellationToken);
    }

    /// Lấy biểu giá chung đang có hiệu lực tại thời điểm chỉ định
    public async Task<IReadOnlyList<TariffResponse>> GetEffectiveAsync(
        long landlordId, long propertyId, DateOnly date,
        CancellationToken cancellationToken)
    {
        await EnsurePropertyOwnershipAsync(landlordId, propertyId, cancellationToken);
        var rates = await db.UtilityTariffs.AsNoTracking()
            .Where(t => t.PropertyId == propertyId
                && t.RoomId == null
                && t.EffectiveFrom <= date)
            .ToListAsync(cancellationToken);

        return TariffSelection.EffectiveForProperty(rates, date)
            .Select(ToResponse).ToArray();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// Kiểm tra quyền sở hữu đối với tòa nhà
    private async Task EnsurePropertyOwnershipAsync(
        long landlordId, long propertyId, CancellationToken cancellationToken)
    {
        var ownsProperty = await db.Properties.AnyAsync(
            p => p.Id == propertyId && p.LandlordId == landlordId, cancellationToken);
        if (!ownsProperty)
            throw ApiException.NotFound("Property.NotFound", "Không tìm thấy căn thuộc tài khoản này.");

    }

    /// Đảm bảo không trùng lặp ngày hiệu lực cho cùng loại dịch vụ
    private async Task EnsureUniqueDateAsync(
        long propertyId, string tariffType, DateOnly date,
        long? exceptId, CancellationToken cancellationToken)
    {
        if (await db.UtilityTariffs.AnyAsync(
            t => t.PropertyId == propertyId && t.RoomId == null
              && t.TariffType == tariffType && t.EffectiveFrom == date
              && t.Id != exceptId, cancellationToken))
            throw ApiException.Conflict("Tariff.DuplicateDate",
                "Đã có đơn giá cùng loại và ngày hiệu lực trong phạm vi này.");
    }

    /// Kiểm tra loại phí và phương thức tính toán hợp lệ
    private static void Validate(string type, string method, long price)
    {
        var valid = type switch
        {
            "ELECTRICITY" => method == "PER_KWH",
            "WATER" => method == "PER_M3",
            "INTERNET" or "SERVICE" or "CLEANING" =>
                method is "PER_PERSON" or "FIXED_ROOM",
            _ => false
        };
        if (!valid || price < 0)
            throw ApiException.BadRequest("Tariff.InvalidInput",
                "Loại phí, cách tính hoặc đơn giá không hợp lệ.");
    }

    private static TariffResponse ToResponse(UtilityTariff tariff) =>
        new(tariff.Id, tariff.PropertyId, tariff.RoomId, tariff.TariffType,
            tariff.CalcMethod, tariff.UnitPrice, tariff.EffectiveFrom);
}
