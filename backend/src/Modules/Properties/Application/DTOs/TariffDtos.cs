namespace HomeMatch.Modules.Properties.Application.DTOs;

/// <summary>Response DTO trả về thông tin đơn giá dịch vụ</summary>
public sealed class TariffResult(
    long id, long propertyId, long? roomId, string tariffType,
    string calcMethod, long unitPrice)
{
    public long Id { get; } = id;
    public long PropertyId { get; } = propertyId;
    public long? RoomId { get; } = roomId;
    public string TariffType { get; } = tariffType;
    public string CalcMethod { get; } = calcMethod;
    public long UnitPrice { get; } = unitPrice;
}
