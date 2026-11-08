namespace HomeMatch.Modules.Properties.Application.DTOs;

public sealed class ProvinceResult(string code, string name, string fullName)
{
    public string Code { get; } = code;
    public string Name { get; } = name;
    public string FullName { get; } = fullName;
}

public sealed class WardResult(string code, string name, string fullName, string provinceCode)
{
    public string Code { get; } = code;
    public string Name { get; } = name;
    public string FullName { get; } = fullName;
    public string ProvinceCode { get; } = provinceCode;
}

public sealed class AmenityResult(long id, string code, string name)
{
    public long Id { get; } = id;
    public string Code { get; } = code;
    public string Name { get; } = name;
}
