using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Billing.Presentation.Endpoints;

public static class BillingEndpoints
{
    public static RouteGroupBuilder MapBillingEndpointsInternal(this RouteGroupBuilder group)
    {
        group.MapGet("/ping", () => Results.Ok(new
        {
            module = "Billing",
            status = "Healthy",
            timestamp = DateTimeOffset.UtcNow
        }))
        .WithName("PingBillingModule")
        .WithSummary("Kiểm tra trạng thái sẵn sàng của phân hệ Billing");

        return group;
    }
}
