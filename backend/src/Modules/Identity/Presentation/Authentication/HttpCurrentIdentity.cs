using HomeMatch.Modules.Identity.Application.Authentication;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Identity.Presentation.Authentication;

public sealed class HttpCurrentIdentity(IHttpContextAccessor accessor) : ICurrentIdentity
{
    public AuthenticatedIdentity? Identity => accessor.HttpContext?.Features.Get<AuthenticatedIdentity>();
}
