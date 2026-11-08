using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints;

internal static class ListingsHttpErrors
{
    public static IResult Failure(Error error) => Results.Json(ApiResponse<object>.Failure(error),
        statusCode: error.Code switch
        {
            "Auth.Unauthorized" or "Auth.Required" => StatusCodes.Status401Unauthorized,
            "Auth.LandlordRequired" or "Auth.TenantRequired" => StatusCodes.Status403Forbidden,
            "Listing.NotFound" or "Room.NotFound" or "Listing.ImageNotFound" => StatusCodes.Status404NotFound,
            "Listing.DuplicateSlug" or "Listing.InvalidStatus" or "Listing.RoomUnavailable"
                or "Listing.RoomAlreadyPublished" or "Listing.LastImage" => StatusCodes.Status409Conflict,
            "Listing.ImageUploadUnavailable" => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status400BadRequest
        });

    public static IResult MissingUser() => Failure(new Error("Auth.Required", "Cần đăng nhập tài khoản."));
    public static IResult MissingLandlord() => Failure(new Error("Auth.LandlordRequired", "Cần tài khoản chủ nhà."));
    public static IResult MissingTenant() => Failure(new Error("Auth.TenantRequired", "Cần tài khoản người thuê."));
}

