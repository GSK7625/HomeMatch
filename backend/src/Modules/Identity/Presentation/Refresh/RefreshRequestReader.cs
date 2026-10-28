using HomeMatch.Modules.Identity.Presentation.Http;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Refresh;

public sealed record RefreshRequest;

public sealed class RefreshRequestReader
{
    public Task<IdentityJsonResult<RefreshRequest>> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
        => IdentityJsonReader.ReadAsync<RefreshRequest>(request,
            root => root.EnumerateObject().Any() ? null : new RefreshRequest(), cancellationToken, new RefreshRequest());
}
