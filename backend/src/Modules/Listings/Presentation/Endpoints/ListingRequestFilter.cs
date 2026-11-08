using System.ComponentModel.DataAnnotations;
using HomeMatch.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace HomeMatch.Modules.Listings.Presentation.Endpoints;

internal sealed class ListingRequestFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var request in context.Arguments.Where(argument => argument?.GetType().Namespace ==
                     "HomeMatch.Modules.Listings.Presentation.Endpoints.Requests"))
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request!, new ValidationContext(request!), errors, true))
                return Results.BadRequest(ApiResponse<object>.Failure(Error.Validation("Listing.InvalidInput",
                    "Thông tin yêu cầu không hợp lệ.", errors.SelectMany(error => error.MemberNames.DefaultIfEmpty(string.Empty)
                        .Select(field => new ErrorDetail(field, error.ErrorMessage ?? "Giá trị không hợp lệ."))).ToArray())));
        }
        return await next(context);
    }
}
