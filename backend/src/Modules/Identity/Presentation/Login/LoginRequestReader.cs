using System.Text.Json;
using HomeMatch.Modules.Identity.Application.Login;
using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Login;

public sealed class LoginRequestReader
{
    public Task<IdentityJsonResult<LoginCommand>> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
        => IdentityJsonReader.ReadAsync<LoginCommand>(request, Parse, cancellationToken);

    private static LoginCommand? Parse(JsonElement root)
    {
        var properties = root.EnumerateObject().ToArray();
        if (properties.Length != 2 || properties.Any(property => property.Name is not ("email" or "password")))
            return null;
        var email = root.GetProperty("email");
        var password = root.GetProperty("password");
        if (email.ValueKind != JsonValueKind.String || password.ValueKind != JsonValueKind.String) return null;
        return new LoginCommand(email.GetString(), password.GetString());
    }
}
