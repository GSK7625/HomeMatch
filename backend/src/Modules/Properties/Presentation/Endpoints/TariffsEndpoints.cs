using System.Security.Claims;
using HomeMatch.Modules.Properties.Application.Features.Tariffs.GetPropertyTariffs;
using HomeMatch.Modules.Properties.Application.Features.Tariffs.SetPropertyTariffs;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Requests;
using HomeMatch.Modules.Properties.Presentation.Endpoints.Responses;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

public static class TariffsEndpoints
{
    public static void MapTariffEndpoints(this RouteGroupBuilder group)
    {
        var tariffGroup = group.MapGroup("/{propertyId:long}/tariffs")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Landlord"))
            .WithTags("Tariffs");

        tariffGroup.MapGet("/", GetPropertyTariffsAsync)
            .WithName("Tariffs.GetList")
            .WithSummary("Lấy danh sách biểu giá dịch vụ của bất động sản")
            .Produces<ApiResponse<IReadOnlyList<TariffResponse>>>(StatusCodes.Status200OK);

        tariffGroup.MapPut("/", SetPropertyTariffsAsync)
            .AddEndpointFilter<PropertyRequestFilter>()
            .DisableValidation()
            .WithName("Tariffs.SetList")
            .WithSummary("Thiết lập hoặc ghi đè toàn bộ biểu giá dịch vụ của bất động sản")
            .Produces<ApiResponse<IReadOnlyList<TariffResponse>>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetPropertyTariffsAsync(
        ClaimsPrincipal user,
        long propertyId,
        GetPropertyTariffsHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();

        var query = new GetPropertyTariffsQuery(landlordId.Value, propertyId);
        var result = await handler.HandleAsync(query, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);

        var response = result.Value.Select(TariffResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }

    private static async Task<IResult> SetPropertyTariffsAsync(
        ClaimsPrincipal user,
        long propertyId,
        [FromBody] SetPropertyTariffsRequest request,
        SetPropertyTariffsHandler handler,
        CancellationToken cancellationToken)
    {
        var landlordId = RequestActor.UserId(user);
        if (landlordId is null) return PropertiesHttpErrors.MissingActor();

        var items = request.Tariffs.Select(t => new TariffItemCommand(t.TariffType, t.CalcMethod, t.UnitPrice)).ToList();
        var command = new SetPropertyTariffsCommand(landlordId.Value, propertyId, items);

        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure) return PropertiesHttpErrors.Failure(result.Error);

        var response = result.Value.Select(TariffResponse.From).ToArray();
        return Results.Ok(ApiResponse.Success(response));
    }
}
