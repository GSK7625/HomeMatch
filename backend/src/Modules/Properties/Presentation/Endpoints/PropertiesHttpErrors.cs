using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Properties.Presentation.Endpoints;

internal static class PropertiesHttpErrors
{
    public static IResult Failure(Error error) => Results.Json(ApiResponse<object>.Failure(error),
        statusCode: error.Code switch
        {
            "Auth.Unauthorized" => StatusCodes.Status401Unauthorized,
            "Auth.LandlordRequired" => StatusCodes.Status403Forbidden,
            "Property.NotFound" or "Room.NotFound" or "Tariff.NotFound" => StatusCodes.Status404NotFound,
            "Room.DuplicateNumber" or "Tariff.DuplicateDate" or "Tariff.AlreadyEffective" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        });

    public static IResult MissingActor() => Failure(new Error("Auth.Unauthorized", "Chưa xác thực danh tính người dùng."));
}
