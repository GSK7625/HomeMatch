namespace HomeMatch.Modules.Properties.Application.DTOs;

public sealed class ProvinceResponse(string code, string name, string fullName)
{
    public string Code { get; } = code;
    public string Name { get; } = name;
    public string FullName { get; } = fullName;
}

public sealed class WardResponse(string code, string name, string fullName, string provinceCode)
{
    public string Code { get; } = code;
    public string Name { get; } = name;
    public string FullName { get; } = fullName;
    public string ProvinceCode { get; } = provinceCode;
}

public sealed class AmenityResponse(long id, string code, string name, string category, string? iconUrl)
{
    public long Id { get; } = id;
    public string Code { get; } = code;
    public string Name { get; } = name;
    public string Category { get; } = category;
    public string? IconUrl { get; } = iconUrl;
}
