namespace HomeMatch.Modules.Properties.Application.DTOs;

/// <summary>Response DTO trả về thông tin phòng</summary>
public sealed class RoomResult(
    long id, long propertyId, string roomNumber, int floor, decimal areaSqm,
    string roomType, int maxOccupants, long basePrice, string status,
    DateOnly availableFrom, IReadOnlyList<long> amenityIds)
{
    public long Id { get; } = id;
    public long PropertyId { get; } = propertyId;
    public string RoomNumber { get; } = roomNumber;
    public int Floor { get; } = floor;
    public decimal AreaSqm { get; } = areaSqm;
    public string RoomType { get; } = roomType;
    public int MaxOccupants { get; } = maxOccupants;
    public long BasePrice { get; } = basePrice;
    public string Status { get; } = status;
    public DateOnly AvailableFrom { get; } = availableFrom;
    public IReadOnlyList<long> AmenityIds { get; } = amenityIds;
}
