using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Logout;

public sealed record LogoutRequest;

public sealed class LogoutRequestReader
{
    public Task<IdentityJsonResult<LogoutRequest>> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
        => IdentityJsonReader.ReadAsync<LogoutRequest>(request,
            root => root.EnumerateObject().Any() ? null : new LogoutRequest(), cancellationToken, new LogoutRequest());
}
