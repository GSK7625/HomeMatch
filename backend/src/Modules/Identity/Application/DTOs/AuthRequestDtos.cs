

namespace HomeMatch.Modules.Identity.Application.DTOs;

public sealed record LoginRequestDto(string? Email, string? Password)
{
    public override string ToString() => "LoginRequestDto { [redacted] }";
}

public sealed record RegistrationRequestDto(string? Email, string? Password, string? FullName, string? Role)
{
    public override string ToString() => "RegistrationRequestDto { [redacted] }";
}
