namespace HomeMatch.Modules.Properties.Application.DTOs;

/// <summary>Response DTO trả về thông tin bất động sản</summary>
public sealed class PropertyResult(
    long id, string name, string provinceCode, string wardCode,
    string addressDetail, string propertyType,
    decimal? latitude = null, decimal? longitude = null)
{
    public long Id { get; } = id;
    public string Name { get; } = name;
    public string ProvinceCode { get; } = provinceCode;
    public string WardCode { get; } = wardCode;
    public string AddressDetail { get; } = addressDetail;
    public string PropertyType { get; } = propertyType;
    public decimal? Latitude { get; } = latitude;
    public decimal? Longitude { get; } = longitude;
}
