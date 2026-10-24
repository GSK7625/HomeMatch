using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMatch.Shared.Abstractions;

public interface IModule
{
    static abstract IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration);
    static abstract IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
