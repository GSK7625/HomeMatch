using HomeMatch.Shared.Common;

namespace HomeMatch.Modules.Billing.Domain.Entities;

/// <summary>
/// Thực thể Ghi chỉ số công tơ Điện & Nước theo kỳ của Phòng / Hợp đồng
/// </summary>
public class UtilityReading
{
    public long Id { get; set; }
    public long PropertyId { get; set; }
    public long RoomId { get; set; }
    public long ContractId { get; set; }
    public long LandlordId { get; set; }
    public long TenantId { get; set; }
    public long RecordedBy { get; set; }

    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }

    public int ElectricityOld { get; set; }
    public int ElectricityNew { get; set; }
    public int ElectricityUsage { get; set; }
    public long ElectricityUnitPrice { get; set; }
    public long ElectricityCost { get; set; }

    public int WaterOld { get; set; }
    public int WaterNew { get; set; }
    public int WaterUsage { get; set; }
    public long WaterUnitPrice { get; set; }
    public long WaterCost { get; set; }

    public long ServiceCost { get; set; }

    public DateOnly ReadingDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static Result<UtilityReading> CreateOrUpdate(
        UtilityReading? existing,
        long propertyId,
        long roomId,
        long contractId,
        long landlordId,
        long tenantId,
        long recordedBy,
        int billingMonth,
        int billingYear,
        int electricityOld,
        int electricityNew,
        long electricityUnitPrice,
        int waterOld,
        int waterNew,
        long waterUnitPrice,
        long serviceCost,
        DateOnly readingDate)
    {
        if (billingMonth < 1 || billingMonth > 12)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.InvalidMonth", "Kỳ tính tiền tháng phải từ 1 đến 12."));
        }

        if (billingYear < 2020)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.InvalidYear", "Kỳ tính tiền năm không hợp lệ."));
        }

        if (electricityOld < 0 || electricityNew < 0)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.InvalidElectricityIndex", "Chỉ số điện không được là số âm."));
        }

        if (electricityNew < electricityOld)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.ElectricityNewLessThanOld", "Chỉ số điện cuối kỳ phải lớn hơn hoặc bằng chỉ số điện đầu kỳ."));
        }

        if (waterOld < 0 || waterNew < 0)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.InvalidWaterIndex", "Chỉ số nước không được là số âm."));
        }

        if (waterNew < waterOld)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.WaterNewLessThanOld", "Chỉ số nước cuối kỳ phải lớn hơn hoặc bằng chỉ số nước đầu kỳ."));
        }

        if (electricityUnitPrice < 0 || waterUnitPrice < 0 || serviceCost < 0)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.InvalidTariffPrice", "Đơn giá và phí dịch vụ không được là số âm."));
        }

        int elecUsage = electricityNew - electricityOld;
        int waterUsage = waterNew - waterOld;

        long elecCost;
        long waterCost;
        try
        {
            elecCost = checked((long)elecUsage * electricityUnitPrice);
            waterCost = checked((long)waterUsage * waterUnitPrice);
            _ = checked(elecCost + waterCost + serviceCost);
        }
        catch (OverflowException)
        {
            return Result.Failure<UtilityReading>(new Error("UtilityReading.AmountOverflow", "Giá trị chỉ số hoặc đơn giá vượt quá giới hạn cho phép."));
        }

        var entity = existing ?? new UtilityReading
        {
            CreatedAt = DateTimeOffset.UtcNow
        };

        entity.PropertyId = propertyId;
        entity.RoomId = roomId;
        entity.ContractId = contractId;
        entity.LandlordId = landlordId;
        entity.TenantId = tenantId;
        entity.RecordedBy = recordedBy;
        entity.BillingMonth = billingMonth;
        entity.BillingYear = billingYear;
        entity.ElectricityOld = electricityOld;
        entity.ElectricityNew = electricityNew;
        entity.ElectricityUsage = elecUsage;
        entity.ElectricityUnitPrice = electricityUnitPrice;
        entity.ElectricityCost = elecCost;
        entity.WaterOld = waterOld;
        entity.WaterNew = waterNew;
        entity.WaterUsage = waterUsage;
        entity.WaterUnitPrice = waterUnitPrice;
        entity.WaterCost = waterCost;
        entity.ServiceCost = serviceCost;
        entity.ReadingDate = readingDate;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success(entity);
    }
}
