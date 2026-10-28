using System.Text.Json;
using HomeMatch.Modules.Identity.Application.Registration;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Registration;

public enum RegistrationReadError { None, Invalid, TooLarge, UnsupportedMediaType }
public sealed record RegistrationReadResult(RegisterAccountCommand? Command, RegistrationReadError Error);

public sealed class RegistrationRequestReader
{
    public const int MaximumBodyBytes = IdentityJsonReader.MaximumBodyBytes;

    public async Task<RegistrationReadResult> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var result = await IdentityJsonReader.ReadAsync<RegisterAccountCommand>(request, Parse, cancellationToken);
        return new(result.Value, result.Error switch
        {
            IdentityJsonError.None => RegistrationReadError.None,
            IdentityJsonError.TooLarge => RegistrationReadError.TooLarge,
            IdentityJsonError.UnsupportedMediaType => RegistrationReadError.UnsupportedMediaType,
            _ => RegistrationReadError.Invalid
        });
    }

    private static RegisterAccountCommand? Parse(JsonElement root)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name is not ("email" or "password" or "fullName" or "role")
                || property.Value.ValueKind != JsonValueKind.String
                || !values.TryAdd(property.Name, property.Value.GetString()!))
                return null;
        }
        return values.Count == 4
            ? new RegisterAccountCommand(values["email"], values["password"], values["fullName"], values["role"]) : null;
    }
}
